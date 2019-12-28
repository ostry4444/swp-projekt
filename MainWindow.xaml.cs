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
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        static SpeechSynthesizer ss;
        static SpeechRecognitionEngine sre;
        static bool done = false;

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
            //Console.WriteLine(Directory.GetCurrentDirectory());
            //Console.WriteLine(Directory.Exists("..\\..\\Grammars"));
            //Grammar grammar = new Grammar(".\\Grammars\\grammar.xml", "rootRule");
            Grammar grammar = new Grammar("..\\..\\Grammars\\grammar.xml", "rootRule");
            grammar.Enabled = true;
            sre.LoadGrammar(grammar);
            sre.RecognizeAsync(RecognizeMode.Multiple);
            while (done == false) {; } //pętla w celu uniknięcia zamknięcia programu
        }

        private static void Sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string txt = e.Result.Text;
            float confidence = e.Result.Confidence;
            if (confidence >= 0.7)
            {
                int first = Convert.ToInt32(e.Result.Semantics["first"].Value);
                int second = Convert.ToInt32(e.Result.Semantics["second"].Value);
                string operation = e.Result.Semantics["operation"].Value.ToString();
                if (operation == "suma")
                {
                    int sum = first + second;
                    ss.Speak("Wynik dodawania wynosi " + sum.ToString());
                }
                else if (operation == "roznica")
                {
                    int sub = first - second;
                    ss.Speak("Wynik odjmowania wynosi " + sub.ToString());
                }
                else if (operation == "iloczyn")
                {
                    int mult = first * second;
                    ss.Speak("Wynik mnożenia wynosi " + mult.ToString());
                }
            }
            else
            {
                ss.Speak("Proszę powtórzyć");
            }

        }
    }
}
