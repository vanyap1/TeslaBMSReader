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


            int[] cells_voltage = { 3880, 0, 0, 0, 0, 0 };
            int[] cells_percent = { 0, 0, 0, 0, 0, 0 };
            double[] aditional_data = { 0, 0, 0 };                                       //Battery voltage, TS1, TS2 

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

            CellVoltage1.Text = cells_voltage[0].ToString("D4") + "mV";
            CellVoltage2.Text = cells_voltage[1].ToString("D4") + "mV";
            CellVoltage3.Text = cells_voltage[2].ToString("D4") + "mV";
            CellVoltage4.Text = cells_voltage[3].ToString("D4") + "mV";
            CellVoltage5.Text = cells_voltage[4].ToString("D4") + "mV";
            CellVoltage6.Text = cells_voltage[5].ToString("D4") + "mV";





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

            if (aditional_data[0] != 0)
                BatVoltage.Text = "BAT: " + (aditional_data[0] / 1000).ToString("F1") + "V";
            if (aditional_data[1] != 0)
                BatTs1.Text = "TS1: " + (aditional_data[1] / 10).ToString("F1") + "�C";
            if (aditional_data[2] != 0)
                BatTs2.Text = "TS2: " + (aditional_data[2] / 10).ToString("F1") + "�C";

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

            byte[] startADC = { 1, 52, 1 };// crc - 88     |  > 3 52 1 23
            byte[] readADC = { 1, 1, 17 };// crc - 88     |  > 3 1 17 223

            //var returnData = WriteDataToPort(data);
            //MessageBox.Show(returnData.ToString());
            string line1 = "";
            string line2 = "";
            string line3 = "";
            string line4 = "";



            line1 = line1 + BitConverter.ToString(serialTx(startADC, true)) + ";";   //127 60 165 58   - 87
            Thread.Sleep(25);
            line2 = line2 + BitConverter.ToString(serialTx(readADC, false)) + ";";
            //line2 = line2 + serialTx(data1).ToString() + ";";  //1 59 199 94     - 
            //line3 = line3 + serialTx(data2).ToString() + ";";  //3 48 61 158     -
            //line4 = line4 + serialTx(data3).ToString() + ";";  //3 49 3 166      - 

            //textBox1.AppendText(cells_voltage[i].ToString() + "; " + PercentCalc(cells_voltage[i]).ToString() + Environment.NewLine);


            textBox1.Text = line1 + Environment.NewLine;
            textBox1.AppendText(line2 + Environment.NewLine);
            textBox1.AppendText(line3 + Environment.NewLine);
            textBox1.AppendText(line4 + Environment.NewLine);


            //byte[] data = await ReadDataFromPortAsync();
            //if (data != null)
            //{
            //    // �������������� �������� ���� �� ��������
            //}


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
                    int timeout = 50; // ������� � ����������
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