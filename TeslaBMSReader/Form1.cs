using System;
using System.Configuration;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using System.Collections;


namespace TeslaBMSReader
{


    public partial class MainForm : Form
    {
        //int[] cells_voltage = new int[6];
        //int[] cells_percent = new int[6];
        private SerialPort _port;


        public MainForm()
        {
            InitializeComponent();
        }




        private void MainForm_Load(object sender, EventArgs e)
        {





            textBox1.AcceptsReturn = true;


           

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = "config.xml";

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            timer1.Enabled = true;

            string SerialPort = config.AppSettings.Settings["SerialPort"].Value;
            string SerialBaud = config.AppSettings.Settings["BaudRate"].Value;
            string OwnerInfo = config.AppSettings.Settings["OwnerInfo"].Value;

            _port = new SerialPort(SerialPort, int.Parse(SerialBaud));
            _port.ReadBufferSize = 1024;
            OpenPort();

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

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] data = { 63, 60, 165 }; //              63 60 165 > 126 60 165 > 127 60 165 + 87
            byte[] data1 = { 0, 59, 129 }; // crc - 139 |  0 59 129 >  0 59 129 >  1 59 129 + 139
            byte[] data2 = { 1, 48, 61 };// crc - 247   |  1 48 61 > 2 48 61 > 3 48 61 + 247
            byte[] data3 = { 1, 49, 3 };// crc - 88     |  1 49 3 > 2 49 3 > 3 49 3 + 88

            int[] cells_voltage = { 3880, 0, 0, 0, 0, 0 };
            double[] aditional_data = { 0, 0, 0 };                                       //Battery voltage, TS1, TS2 


            byte[] startADC = { 1, 52, 1 };// crc - 88     |  > 3 52 1 23
            byte[] readADC = { 1, 1, 17 };// crc - 88     |  > 3 1 17 223

            //var returnData = WriteDataToPort(data);
            //MessageBox.Show(returnData.ToString());
            string line1 = "";
            string line2 = "";
            string line3 = "";
            string line4 = "";

            byte[] battery_read_data = serialTx(startADC, true);
            
            line1 = line1 + BitConverter.ToString(battery_read_data) + ";";   //127 60 165 58   - 87
            Thread.Sleep(25);
            battery_read_data = serialTx(readADC, false);
            line2 = line2 + BitConverter.ToString(battery_read_data) + ";";

            if ((battery_read_data[0] == 0x02 && battery_read_data[1] == 0x01 && battery_read_data[2] == 0x11)) //02-01-11
            {
                int[] numbers = { 5, 7, 9, 11, 13, 15 };
                byte cellIndex = 0;
                foreach (int n in numbers)
                {
                    double voltage = Math.Round((battery_read_data[n] * 256 + battery_read_data[n + 1]) * 6.250 / 16383, 3);
                    cells_voltage[cellIndex] = Convert.ToInt32(voltage * 1000);
                    cellIndex++;

                    line3 = line3 + "Cell voltage: " + voltage.ToString("F3") + Environment.NewLine;
                }
                drav_display(cells_voltage, aditional_data);
            }
            else
            {
                line3 = "something else";
            }//


            textBox1.Text = line1 + Environment.NewLine;
            textBox1.AppendText(line2 + Environment.NewLine);
            textBox1.AppendText(line3 + Environment.NewLine);
            textBox1.AppendText(line4 + Environment.NewLine);

        }

        public void drav_display(int[] cells_data , double[] extra_data)
        {
            CellVoltage1.Text = cells_data[0].ToString("D4") + "mV";
            CellVoltage2.Text = cells_data[1].ToString("D4") + "mV";
            CellVoltage3.Text = cells_data[2].ToString("D4") + "mV";
            CellVoltage4.Text = cells_data[3].ToString("D4") + "mV";
            CellVoltage5.Text = cells_data[4].ToString("D4") + "mV";
            CellVoltage6.Text = cells_data[5].ToString("D4") + "mV";

            //for (int i = 0; i <= 5; i++)
            //{
            //    textBox1.AppendText(cells_data[i].ToString() + "; " + PercentCalc(cells_data[i]).ToString() + Environment.NewLine);
            //}

            batFullBox1.Height = batEmptyBox1.Height - PercentCalc(cells_data[0]) * 2;
            batFullBox2.Height = batEmptyBox1.Height - PercentCalc(cells_data[1]) * 2;
            batFullBox3.Height = batEmptyBox1.Height - PercentCalc(cells_data[2]) * 2;
            batFullBox4.Height = batEmptyBox1.Height - PercentCalc(cells_data[3]) * 2;
            batFullBox5.Height = batEmptyBox1.Height - PercentCalc(cells_data[4]) * 2;
            batFullBox6.Height = batEmptyBox1.Height - PercentCalc(cells_data[5]) * 2;

            MaximumVal.Text = "MAX: " + cells_data.Max() + "mV";
            MinimumVal.Text = "MIN: " + cells_data.Min() + "mV";
            DifferenceVal.Text = "DIFF: " + (cells_data.Max() - cells_data.Min()) + "mV";

            if (extra_data[0] != 0)
                BatVoltage.Text = "BAT: " + (extra_data[0] / 1000).ToString("F1") + "V";
            if (extra_data[1] != 0)
                BatTs1.Text = "TS1: " + (extra_data[1] / 10).ToString("F1") + "°C";
            if (extra_data[2] != 0)
                BatTs2.Text = "TS2: " + (extra_data[2] / 10).ToString("F1") + "°C";
        }


        private byte[] serialTx(byte[] data, bool need_crc)
        {
            var indata = data;
            byte crc = 0;
            data[0] = (byte)(data[0] << 1);
            if (need_crc)
            {
                data[0] = (byte)(data[0] | 0x01);
                crc = CalculateCRC8(data);
                Array.Resize(ref data, data.Length + 1);
                data[data.Length - 1] = crc;
            }

            return WriteDataToPort(data);

        }


        private byte CalculateCRC8(byte[] data)
        {
            {
                byte generator = 0x07;
                byte crc = 0;

                for (int x = 0; x < data.Length; x++)
                {
                    crc ^= data[x]; // XOR-in the next input byte

                    for (int i = 0; i < 8; i++)
                    {
                        if ((crc & 0x80) != 0)
                        {
                            crc = (byte)((crc << 1) ^ generator);
                        }
                        else
                        {
                            crc <<= 1;
                        }
                    }
                }

                return crc;
            }
        }


        //Serial console section

        private void OpenPort()
        {
            try
            {
                _port.Open();
                button1.BackColor = Color.Lime;
            }
            catch (IOException ex)
            {
                button1.BackColor = Color.Red;
            }
        }


        private byte[] WriteDataToPort(byte[] data)
        {
            List<byte> data_read = new List<byte>();
            if (_port.IsOpen)
            {
                try
                {
                    _port.Write(data, 0, data.Length);
                    Thread.Sleep(25);
                    int timeout = 50; // Таймаут у мілісекундах
                    DateTime startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
                    {
                        if (_port.BytesToRead > 0)
                        {
                            int bytesRead = _port.BytesToRead;
                            byte[] buffer = new byte[bytesRead];
                            _port.Read(buffer, 0, bytesRead);
                            data_read.AddRange(buffer);
                        }
                    }

                    return data_read.ToArray();
                }
                catch (IOException ex)
                {
                    return data_read.ToArray();
                }
            }
            else
            {
                return data_read.ToArray();
            }
        }



    }
}