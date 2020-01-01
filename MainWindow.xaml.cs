using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Globalization;
using System.IO;
using System.ComponentModel;

namespace swp_projekt
{

    public class TaxiOrder
    {
        public TaxiOrder(){
            //dateTime = new DateTime();
        }
        public string address { get; set; }
        public string time { get; set; }
        public string date { get; set; }
        //public DateTime dateTime { get; set; }
        public int seats { get; set; }
        public int phone { get; set; }
        public string name { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        static SpeechSynthesizer ss;
        static SpeechRecognitionEngine sre;
        static bool done = false;

        static TaxiOrder taxiOrder = new TaxiOrder();
        
        public MainWindow()
        {
            InitializeComponent(); 

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ss = new SpeechSynthesizer();
            ss.SetOutputToDefaultAudioDevice();
            ss.Speak("Witam w taxi");
            CultureInfo ci = new CultureInfo("pl-PL"); //ustawienie języka
            sre = new SpeechRecognitionEngine(ci); //powołanie engine rozpoznawania
            sre.SetInputToDefaultAudioDevice(); //ustawienie domyślnego urządzenia wejściowego
            sre.SpeechRecognized += Sre_SpeechRecognized;
            Grammar grammar = new Grammar("..\\..\\Grammars\\grammar.xml", "orderTaxiRule");            
            //Grammar grammar = new Grammar("..\\..\\Grammars\\grammar.xml", "number");
            
            grammar.Enabled = true;
            sre.LoadGrammar(grammar);
            sre.RecognizeAsync(RecognizeMode.Multiple);
            while (done == false) {; } //pętla w celu uniknięcia zamknięcia programu
        }

        private /*static*/ void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string txt = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.5){
                Console.WriteLine(e.Result.Text);

                if (txt.IndexOf("Poproszę") >= 0) {
                    // new order
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        clearForm();
                    })); 
                    taxiOrder = new TaxiOrder();

                    getAddress(e);  // grammar ! addrress string https://stackoverflow.com/questions/15913781/add-generic-placeholders-to-srgs-grammar  
                    getTime(e);
                    getDate(e);
                    getSeats(e);
                    getPhone(e);
                    getName(e);     // grammar ! name string

                    refineOrder();
                }
                else {
                    if (String.IsNullOrEmpty(taxiOrder.address))
                        getAddress(e);
                    else if (String.IsNullOrEmpty(taxiOrder.time))
                        getTime(e);
                    else if (String.IsNullOrEmpty(taxiOrder.date))
                        getDate(e);
                    else if (taxiOrder.seats == 0) 
                        getSeats(e);
                    else if (taxiOrder.phone == 0)
                        getPhone(e);
                    else if (String.IsNullOrEmpty(taxiOrder.name))
                        getName(e);

                    refineOrder();
                }

            }
            else { // low confidence
                ss.Speak("Proszę powtórzyć");
            }

        }

        private void newOrder_Click(object sender, RoutedEventArgs e){
            clearForm();
        }

        private void confirm_Click(object sender, RoutedEventArgs e){
            // TODO : save taxiOrder to DB
            tb_INFO.Text = "ZAMÓWIONO TAKSÓWKĘ";
        }

        private void clearForm()
        {
            label_address.Background = label_time.Background = label_date.Background = label_seats.Background = label_phone.Background = label_name.Background = Brushes.Transparent;
            tb_address.Text = tb_time.Text = tb_date.Text = tb_seats.Text = tb_phone.Text = tb_name.Text = "";
            tb_INFO.Text = "";
            tb.Text = "";
        }

        private void refineOrder()
        {
            if (String.IsNullOrEmpty(taxiOrder.address)) {
                ss.SpeakAsync("Na jaki adres wysłać taksówkę? ");
                statusAsk(label_address);
            }
            else if (String.IsNullOrEmpty(taxiOrder.time)) { //taxiOrder.dateTime.? != ?
                ss.SpeakAsync("O której godzinie ma przyjechać taksówka? ");
                statusAsk(label_time);
            }
            else if (String.IsNullOrEmpty(taxiOrder.date)) { //taxiOrder.dateTime.Year!=1
                ss.SpeakAsync("W jaki dzień ma przyjechać taksówka? ");
                statusAsk(label_date);
            }
            else if (taxiOrder.seats == 0){
                ss.SpeakAsync("na ile osób taksówka? ");
                statusAsk(label_seats);
            }
            else if (taxiOrder.phone == 0){
                ss.SpeakAsync("poporoszę numer telefonu ");
                statusAsk(label_phone);
            }
            else if (String.IsNullOrEmpty(taxiOrder.name)){
                ss.SpeakAsync("pani/pana godność?? ");
                statusAsk(label_name);
            }
            else
                doOrder();
            
        }

        private void statusAsk(Label label)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                label.Background = Brushes.Yellow;
            }));
        }
        private void statusOK(Label label, TextBox tb, String text)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                label.Background = Brushes.LightGreen;
                tb.Text = text;
            }));
        }
        private void doOrder()
        {
            Console.WriteLine(taxiOrder.ToString());
            ss.SpeakAsync("zamówiono taksówkę" );
                        
            this.Dispatcher.BeginInvoke(new Action(() => {
                confirm_Click(null, null);
                clearForm();
            }));
        }


        private void getAddress(SpeechRecognizedEventArgs e)
        {
            String address = null;
            int addressNumber = -1;
            try {
                address = e.Result.Semantics["address"].Value.ToString();
                Console.WriteLine("address: " + address);
            }
            catch (Exception ex){
                Console.WriteLine("address missing");
            }

            try {
                addressNumber = Convert.ToInt32(e.Result.Semantics["anum"].Value);
                Console.WriteLine("addressNumber: "+ addressNumber);
            }
            catch (Exception ex){
                Console.WriteLine("addressNumber missing");
            }

            if (addressNumber != -1 && !String.IsNullOrEmpty(address) ){
                taxiOrder.address = address + " " + addressNumber; 
                statusOK(label_address, tb_address, taxiOrder.address);                
            }
        }

        private void getTime(SpeechRecognizedEventArgs e)
        {
            int hour = -1;
            int minute = -1;
            try{
                hour = Convert.ToInt32(e.Result.Semantics["hour"].Value);
                Console.WriteLine("hour: "+hour);
            }
            catch (Exception ex){
                Console.WriteLine("hour missing");
            }
            try{
                minute = Convert.ToInt32(e.Result.Semantics["minute"].Value);
                Console.WriteLine("minute: " + minute);
            }
            catch (Exception ex){
                Console.WriteLine("minute missing");
            }

            if (hour != -1 && minute != -1) {
                taxiOrder.time= hour +":"+ minute;
                statusOK(label_time, tb_time, taxiOrder.time);
            }

        }

        private void getDate(SpeechRecognizedEventArgs e)
        {   // dziś / jutro / pojutrze / dd.mm 
            String date = null;
            try {
                date = e.Result.Semantics["date"].Value.ToString();
                Console.WriteLine("date: "+date );
            }
            catch (Exception ex) {
                Console.WriteLine("date missing");
            }

            if (!String.IsNullOrEmpty(date)){
                // int hour = taxiOrder.dateTime.Hour;
                // int minute = taxiOrder.dateTime.Minute;

                if (date.Length < 5) {
                    if (date.Equals("0")) {
                        taxiOrder.date = DateTime.Now.Day.ToString() + "." + DateTime.Now.Month.ToString();
                        // taxiOrder.dateTime = DateTime.Now; // overwrite hour
                    }else if (date.Equals("+1")){
                        taxiOrder.date = DateTime.Now.AddDays(1).Day.ToString() + "." + DateTime.Now.AddDays(1).Month.ToString();
                        // taxiOrder.dateTime. = DateTime.Now.AddDays(1);
                    }
                    else if (date.Equals("+2")){
                        taxiOrder.date = DateTime.Now.AddDays(2).Day.ToString() + "." + DateTime.Now.AddDays(2).Month.ToString();
                        // taxiOrder.dateTime = DateTime.Now.AddDays(2);
                    }
                }
                else { // exact date
                    taxiOrder.date = date;

                    // int day = Convert.ToInt32(date.Substring(0, 2));
                    // int month = Convert.ToInt32(date.Substring(3, 2));                    
                    // taxiOrder.dateTime = new DateTime(DateTime.Now.Year, month, day, hour, minute, 0); 
                }

                //tb_date.Text = taxiOrder.dateTime.Day.ToString() +"."+ taxiOrder.dateTime.Month.ToString();
                statusOK(label_date, tb_date, taxiOrder.date);
            }
        }

        private void getSeats(SpeechRecognizedEventArgs e)
        {
            int seats = 0;
            try{
                seats = Convert.ToInt32(e.Result.Semantics["passangers"].Value);
                Console.WriteLine("passangers: " + seats);
            }
            catch (Exception ex){
                Console.WriteLine("passangers missing");
            }

            if (seats != 0){
                taxiOrder.seats = seats;
                statusOK(label_seats, tb_seats, seats.ToString());
            }
        }

        private void getPhone(SpeechRecognizedEventArgs e)
        {
            int phone = 0;
            try { 
                phone = Convert.ToInt32(e.Result.Semantics["phone"].Value);
                Console.WriteLine("phone: " + phone);
            }
            catch (Exception ex){
                Console.WriteLine("phone missing");
            }

            if (phone != 0){
                taxiOrder.phone = phone;
                statusOK(label_phone, tb_phone, taxiOrder.phone.ToString());
            }
        }

        private void getName(SpeechRecognizedEventArgs e)
        {
            // throw new NotImplementedException();
        }

    }
}
