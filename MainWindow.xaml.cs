using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Media;
using System.Windows.Media.Imaging;

namespace swp_projekt
{
    public class TaxiOrder { 
        public string street { get; set; }
        public string addressNumber { get; set; }
        public string hour { get; set; }
        public string minute { get; set; }
        public string day { get; set; }
        public string month { get; set; }
        public string dateTimeStr { get; set; }
        public int seats { get; set; }
        public int phone { get; set; }

        public DateTime getDateTime(){
            try{
                return new DateTime(DateTime.Now.Year, Convert.ToInt32(this.month), Convert.ToInt32(this.day), Convert.ToInt32(this.hour), Convert.ToInt32(this.minute), 0);
            }
            catch(Exception e){
                Console.WriteLine("! date or time missing");
            }
            return new DateTime(); 
        }

        public override string ToString(){
            return street + " " + addressNumber + "; " + hour + ":" + minute + "; " + day+"."+month + "; " + seats + "; " + phone ;
        }
    }

    public partial class MainWindow : Window
    {
        static SpeechSynthesizer ss = new SpeechSynthesizer();
        static SpeechRecognitionEngine sre;
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        static bool done = false;

        static TaxiOrder taxiOrder = new TaxiOrder();
        private List<string> streets;

        Grammar grammarStreet, grammarAddressNumber, grammarDate, grammarTime, grammarPhone, grammarPassangers; 

        public MainWindow()
        {
            if (!DBconnector.connectionTest()){
                MessageBoxResult result = MessageBox.Show("Unable to connect to database. \nCheck Internet connection.",
                                          "ERROR",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                Environment.Exit(1);
            } 
            streets = DBconnector.ReadStreets();
            Console.WriteLine("no streets: " + streets.Count);

            InitializeComponent();

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ss.SetOutputToDefaultAudioDevice();
            ss.Speak("Witam w taxi. Na jaką ulice wysłać taksówkę? ");
            CultureInfo ci = new CultureInfo("pl-PL"); 
            sre = new SpeechRecognitionEngine(ci);
            sre.SetInputToDefaultAudioDevice();
            sre.SpeechRecognized += Sre_SpeechRecognized;

            GrammarBuilder buildGrammarSystem = new GrammarBuilder();
            buildGrammarSystem.Append("nowe");
            Grammar grammarSystem = new Grammar(buildGrammarSystem); 
            sre.LoadGrammar(grammarSystem);
            grammarSystem.Enabled = true;

            GrammarBuilder grammarStreetBuilder = new GrammarBuilder();
            Choices streetChoises = new Choices();
            foreach (string s in streets)
                streetChoises.Add(s);

            grammarStreetBuilder.Append(new SemanticResultKey("street", streetChoises), 1, 1);
            grammarStreet = new Grammar(grammarStreetBuilder);
            sre.LoadGrammar(grammarStreet);
            grammarStreet.Enabled = true;

            grammarAddressNumber    = new Grammar("Grammars\\grammarAddressNumber.xml", "orderTaxiRule");
            grammarDate             = new Grammar("Grammars\\grammarDate.xml", "orderTaxiRule");
            grammarTime             = new Grammar("Grammars\\grammarTime.xml", "orderTaxiRule");
            grammarPassangers       = new Grammar("Grammars\\grammarPassangers.xml", "orderTaxiRule");
            grammarPhone            = new Grammar("Grammars\\grammarPhone.xml", "orderTaxiRule");

            sre.LoadGrammar(grammarAddressNumber);
            sre.LoadGrammar(grammarDate);
            sre.LoadGrammar(grammarTime);
            sre.LoadGrammar(grammarPassangers);
            sre.LoadGrammar(grammarPhone);
            
            sre.RecognizeAsync(RecognizeMode.Multiple);
            while (done == false) {; } 
        }

        private void disableGrammars()
        {
            grammarStreet.Enabled = grammarAddressNumber.Enabled = grammarDate.Enabled = grammarTime.Enabled = grammarPhone.Enabled = grammarPassangers.Enabled = false;
        }

        private void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string txt = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.8){
                Console.WriteLine(e.Result.Text);


                if (txt.IndexOf("nowe") >= 0) {
                    Console.WriteLine("new order");
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        newOrder_Click(null, null);
                    })); 

                    taxiOrder = new TaxiOrder();
                    refineOrder();
                }
                else{
                    if (String.IsNullOrEmpty(taxiOrder.street))
                        getStreet(e);
                    else if (String.IsNullOrEmpty(taxiOrder.addressNumber))
                        getAddressNumber(e);
                    else if (String.IsNullOrEmpty(taxiOrder.hour))
                        getTime(e);
                    else if (String.IsNullOrEmpty(taxiOrder.day))
                        getDate(e);
                    else if (taxiOrder.seats == 0) 
                        getSeats(e);
                    else if (taxiOrder.phone == 0)
                        getPhone(e);
                    
                    refineOrder();
                }

            }
            else { // low confidence
                ss.Speak("Proszę powtórzyć");
            }
        }
        
        private void newOrder_Click(object sender, RoutedEventArgs e){
            clearForm();
            taxiOrder = new TaxiOrder();
            disableGrammars();
            grammarStreet.Enabled = true;
        }

        private void confirm_Click(object sender, RoutedEventArgs e){

            if (DBconnector.InsertTaxiOrder(taxiOrder))
            {
                ss.Speak("zamówiono taksówkę.");
                tb_INFO.Text = "ZAMÓWIONO TAKSÓWKĘ. \n Aby złożyć kolejne zamówienie powiedz 'nowe'";
                
                SoundPlayer player = new SoundPlayer("Resources/car_racing-14db.wav");
                player.LoadCompleted += delegate (object s, AsyncCompletedEventArgs ee) {
                    player.Play();
                };
                player.LoadAsync();
            }
            else
            {
                Console.WriteLine("! problem with saving to database taxiOrder ");
            }
        }

        private void clearForm()
        {
            label_address.Background = label_time.Background = label_date.Background = label_seats.Background = label_phone.Background = Brushes.Transparent;
            tb_address.Text = tb_time.Text = tb_date.Text = tb_seats.Text = tb_phone.Text = ""; 
            tb_INFO.Text = "";
        }

        private void refineOrder()
        {
            if (String.IsNullOrEmpty(taxiOrder.street))
            {
                ss.SpeakAsync("Na jaką ulice wysłać taksówkę? ");
                statusAsk(label_address);
            }
            else if (String.IsNullOrEmpty(taxiOrder.addressNumber)){
                ss.SpeakAsync("poproszę numer budynku ");
                statusAsk(label_address);
            }
            else if (String.IsNullOrEmpty(taxiOrder.hour)) { 
                ss.SpeakAsync("O której godzinie ma przyjechać taksówka? ");
                statusAsk(label_time);
            }
            else if (String.IsNullOrEmpty(taxiOrder.day)) { 
                ss.SpeakAsync("W jaki dzień ma przyjechać taksówka? ");
                statusAsk(label_date);
            }
            else if (taxiOrder.seats == 0){
                ss.SpeakAsync("na ile osób taksówka? ");
                statusAsk(label_seats);
            }
            else if (taxiOrder.phone == 0){
                ss.SpeakAsync("poporoszę numer telefonu ");
                hint("podaj numer w postaci 9-ciu cyfr");
                statusAsk(label_phone);
            }
            else
                doOrder();            
        }

        private void setTextBox(TextBox tb, string text)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.Text = text;
            }));
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
        private void hint(String text)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                tb_INFO.Text = text;
            }));
        }
        
        private void doOrder()
        {
           this.Dispatcher.BeginInvoke(new Action(() => {
                confirm_Click(null, null);
                // clearForm();
           }));            
        }


        private void getStreet(SpeechRecognizedEventArgs e)
        {
            String address = null;
            try{
                address = e.Result.Semantics["street"].Value.ToString();
                Console.WriteLine("db street: " + address);
            }
            catch (Exception ex){
                Console.WriteLine("db street missing");
            }
            if (!String.IsNullOrEmpty(address))
            {
                taxiOrder.street = address;
                setTextBox(tb_address, taxiOrder.street);
                disableGrammars();
                grammarAddressNumber.Enabled = true;
            }

        }

        private void getAddressNumber(SpeechRecognizedEventArgs e)
        {
            int addressNumber = -1;
            String numberAB = null;

            try{
                addressNumber = Convert.ToInt32(e.Result.Semantics["anum"].Value);
                Console.WriteLine("addressNumber: " + addressNumber);
            }
            catch (Exception ex){
                Console.WriteLine("addressNumber missing");
            }
            try{
                numberAB = e.Result.Semantics["ab"].Value.ToString();
                Console.WriteLine("numberAB: " + numberAB);
            }
            catch (Exception ex){
                Console.WriteLine("numberAB missing");
            }

            if (!string.IsNullOrEmpty(taxiOrder.street) && addressNumber != -1)
            {
                if (DBconnector.getLatLong(taxiOrder.street.ToString(), addressNumber.ToString()+numberAB)[0]!=0)
                {
                    if (!string.IsNullOrEmpty(numberAB))
                        taxiOrder.addressNumber = addressNumber.ToString() + numberAB;
                    else
                        taxiOrder.addressNumber = addressNumber.ToString();

                    statusOK(label_address, tb_address, taxiOrder.street + " " + taxiOrder.addressNumber);
                    double[] latlong = DBconnector.getLatLong(taxiOrder.street, taxiOrder.addressNumber);
                    //double[] latlong = { 52.179057, 20.998776 };
                    setPushpin(latlong[0], latlong[1]);
                    disableGrammars();
                    grammarTime.Enabled = true;
                }
                else
                {
                    ss.SpeakAsync("adres o podanym numerze nie istnieje");
                }
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
                if (minute >= 60) minute = -1;
            }
            catch (Exception ex){
                Console.WriteLine("minute missing");
            }

            if (hour != -1 && minute != -1){
                taxiOrder.hour = hour.ToString();
                taxiOrder.minute = minute.ToString();
                if (taxiOrder.minute.Length == 1)
                    taxiOrder.minute = "0" + taxiOrder.minute;

                statusOK(label_time, tb_time, taxiOrder.hour+":"+taxiOrder.minute);
                disableGrammars();
                grammarDate.Enabled = true;
            }

        }

        private void getDate(SpeechRecognizedEventArgs e)
        {   // dziś / jutro / pojutrze / dd.mm 
            String date = null;
            try {
                date = e.Result.Semantics["date"].Value.ToString();
                Console.WriteLine("date: "+ date );
            }
            catch (Exception ex) {
                Console.WriteLine("date missing");
            }

            if (!String.IsNullOrEmpty(date)){

                if (date.Length < 3) {
                    if (date.Equals("0")) { 
                        taxiOrder.day = DateTime.Now.Day.ToString();
                        taxiOrder.month = DateTime.Now.Month.ToString();
                    } 
                    else if (date.Equals("+1")) {
                        taxiOrder.day = DateTime.Now.AddDays(1).Day.ToString();
                        taxiOrder.month = DateTime.Now.AddDays(1).Month.ToString();
                    }
                    else if (date.Equals("+2")) {
                        taxiOrder.day = DateTime.Now.AddDays(2).Day.ToString();
                        taxiOrder.month = DateTime.Now.AddDays(2).Month.ToString();
                    }
                }
                else { // exact date
                    string[] dd_mm = date.Split('.');
                    taxiOrder.day = dd_mm[0];
                    taxiOrder.month = dd_mm[1];
                }
                if (taxiOrder.day.Length == 1)
                    taxiOrder.day = "0" + taxiOrder.day;
                if (taxiOrder.month.Length == 1)
                    taxiOrder.month = "0" + taxiOrder.month;

                statusOK(label_date, tb_date, taxiOrder.day+"."+taxiOrder.month);
                disableGrammars();
                grammarPassangers.Enabled = true;
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
                grammarPhone.Enabled = true;
            }
        }

        private void getPhone(SpeechRecognizedEventArgs e)
        {
            int phone = 0;
            try{
                phone = Convert.ToInt32(e.Result.Semantics["phone"].Value);                 
                Console.WriteLine("phone: " + phone );
            }
            catch (Exception ex){
                Console.WriteLine("phone missing");
            }

            if (phone != 0){
                taxiOrder.phone = phone;
                statusOK(label_phone, tb_phone, taxiOrder.phone.ToString());
                disableGrammars();
            }
        }
        private void setPushpin(double lat, double lon)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                pushpin.Location.Latitude = lat;
                pushpin.Location.Longitude = lon;
                Microsoft.Maps.MapControl.WPF.Location location = new Microsoft.Maps.MapControl.WPF.Location(lat, lon);
                map.SetView(location, 16);

            }));
        }

        Object tb_bgr;
        private void tb_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (tb.Background.Equals(Brushes.Transparent)){                
                ImageBrush ib = (ImageBrush) tb_bgr;
                ib.Stretch = Stretch.UniformToFill;
                tb.Text = "";
                tb.Background = ib;
            }
            else{
                tb_bgr = tb.Background;
                List<TaxiOrder> taxiOrders = DBconnector.getTaxiOrders();
                tb.Background = Brushes.Transparent;
                String orders = "> address; date; seats; phone";
                foreach (TaxiOrder taxiOrder in taxiOrders)
                    orders += "\n" + taxiOrder.street + "; " + taxiOrder.dateTimeStr + "; " + taxiOrder.seats + "; " + taxiOrder.phone;

                tb.Text = orders; 
            }
        }

    }
}
