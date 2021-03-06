﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;



namespace Scrambler
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!File.Exists("config/scrambler.txt") || (new FileInfo("config/scrambler.txt").Length == 0))
            {
                FileStream Ccreator = new FileStream("config/scrambler.txt", FileMode.OpenOrCreate);
                Ccreator.Close();

                System.IO.StreamWriter Cwriter = new System.IO.StreamWriter("config/scrambler.txt");
                Cwriter.WriteLine("PoeHUD.exe");
                Cwriter.Close();
            }
            string HUDLOC = System.IO.File.ReadLines("config/scrambler.txt").First();
            if (!File.Exists(HUDLOC))
            {
                OpenFileDialog finder = new OpenFileDialog();
                finder.InitialDirectory = Application.StartupPath;
                finder.Filter = "executable files (*.exe)|*.exe";
                finder.RestoreDirectory = true;
                MessageBox.Show("Couldn't find "+HUDLOC+" , please choose the HUD executable, this will set it as the new base executable so you don't have to pick every time."); 

                if (finder.ShowDialog() == DialogResult.OK)
                {
                    HUDLOC = finder.SafeFileName;
                    System.IO.StreamWriter savehudex = new System.IO.StreamWriter("config/scrambler.txt");
                    savehudex.WriteLine(HUDLOC);
                    savehudex.Close();
                }
                else
                {
                    Console.WriteLine("You haven't chosen a file, or chose an inproper file please try again.");
                    Console.ReadKey();
                    return;
                }


            }
            ushort Csum = Scrambler.GetCSum(HUDLOC);
            string hash = Csum.ToString();
            if (hash == "0") {
                Console.WriteLine("This is your first time running the program, This will randomise the hash of the program by editing the PE header so that even if the executable is scanned, Remember to rename the executable. \n");
            }
            Console.WriteLine("Curent hash " + hash + "\n Press Any Key to continue. \n");
            Random rnd = new Random();
            ushort randu = (ushort)rnd.Next(0, 65535);
            Scrambler.ScrambleCsum(HUDLOC, HUDLOC, randu);
            Csum = Scrambler.GetCSum(HUDLOC);
            hash = Csum.ToString();
            Console.WriteLine("New hash " + hash +"\n This window will close in 3 secconds.");
            Thread.Sleep(3000);
            if (args.Length == 0) 
            {
                return;
            }
            if (args[0] == "fromHUD")
            {
                System.Diagnostics.Process.Start(HUDLOC);
            }
            else
            {
                MessageBox.Show("Invalid arguments passed to scrambler");
                return;
            }
        }
    }
}
