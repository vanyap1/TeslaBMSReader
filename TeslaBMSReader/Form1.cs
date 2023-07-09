using System;
using System.Configuration;
using System.Reflection.Metadata.Ecma335;
using System.Linq;

namespace TeslaBMSReader
{
    public partial class MainForm : Form
    {
        //int[] cells_voltage = new int[6];
        //int[] cells_percent = new int[6];


        public MainForm()
        {
            InitializeComponent();
        }




        private void MainForm_Load(object sender, EventArgs e)
        {
            textBox1.AcceptsReturn = true;


            int[] cells_voltage = { 3761, 4092, 3780, 4200, 4000, 3825 };
            int[] cells_percent = { 0 };
            double[] aditional_data = { 22261, 252, 251 };                                       //Battery voltage, TS1, TS2 

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = "config.xml";

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            timer1.Enabled = true;



            string SerialPort = config.AppSettings.Settings["SerialPort"].Value;
            string SerialBaud = config.AppSettings.Settings["BaudRate"].Value;
            string OwnerInfo = config.AppSettings.Settings["OwnerInfo"].Value;

            textBox1.AppendText(SerialPort + Environment.NewLine);
            textBox1.AppendText(SerialBaud + Environment.NewLine);
            textBox1.AppendText(OwnerInfo + Environment.NewLine);





            batEmptyBox1.BackgroundImage = Properties.Resources.bat_full;
            batEmptyBox2.BackgroundImage = Properties.Resources.bat_full;
            batEmptyBox3.BackgroundImage = Properties.Resources.bat_full;
            batEmptyBox4.BackgroundImage = Properties.Resources.bat_full;
            batEmptyBox5.BackgroundImage = Properties.Resources.bat_full;
            batEmptyBox6.BackgroundImage = Properties.Resources.bat_full;



            batFullBox1.BackgroundImage = Properties.Resources.bat_empty;
            batFullBox2.BackgroundImage = Properties.Resources.bat_empty;
            batFullBox3.BackgroundImage = Properties.Resources.bat_empty;
            batFullBox4.BackgroundImage = Properties.Resources.bat_empty;
            batFullBox5.BackgroundImage = Properties.Resources.bat_empty;
            batFullBox6.BackgroundImage = Properties.Resources.bat_empty;




            CellVoltage1.Text = cells_voltage[0].ToString();
            CellVoltage2.Text = cells_voltage[1].ToString();
            CellVoltage3.Text = cells_voltage[2].ToString();
            CellVoltage4.Text = cells_voltage[3].ToString();
            CellVoltage5.Text = cells_voltage[4].ToString();
            CellVoltage6.Text = cells_voltage[5].ToString();





            for (int i = 0; i <= 5; i++)
            {
                textBox1.AppendText(cells_voltage[i].ToString() + "; " + PercentCalc(cells_voltage[i]).ToString() + Environment.NewLine);
            }

            batFullBox1.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[0]) * 2;
            batFullBox2.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[1]) * 2;
            batFullBox3.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[2]) * 2;
            batFullBox4.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[3]) * 2;
            batFullBox5.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[4]) * 2;
            batFullBox6.Height = batEmptyBox1.Height - PercentCalc(cells_voltage[5]) * 2;

            MaximumVal.Text = "MAX: " + cells_voltage.Max() + "mV";
            MinimumVal.Text = "MIN: " + cells_voltage.Min() + "mV";
            DifferenceVal.Text = "DIFF: " + (cells_voltage.Max() - cells_voltage.Min()) + "mV";
            BatVoltage.Text = "BAT: " + (aditional_data[0] / 1000).ToString("F1") + "V";
            BatTs1.Text = "TS1: " + (aditional_data[1] / 10).ToString("F1") + "°C";
            BatTs2.Text = "TS2: " + (aditional_data[2] / 10).ToString("F1") + "°C";

        }


        public int PercentCalc(int voltage)
        {
            if (voltage < 3700)
                return 0;

            if (voltage > 4200)
                return 100;

            if (voltage >= 3700 && voltage <= 4200)
            {
                voltage = (voltage - 3700) / 5;
            }
            return voltage;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeLbl.Text = DateTime.Now.ToString();
        }
    }
}