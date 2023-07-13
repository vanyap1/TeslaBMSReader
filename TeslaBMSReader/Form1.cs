using System.Configuration;
using System.IO.Ports;

namespace TeslaBMSReader
{


    public partial class MainForm : Form
    {
        //int[] cells_voltage = new int[6];
        //int[] cells_percent = new int[6];
        private SerialPort _port;
        public byte BROADCAST = 0x3F;
        public byte ADDRESS_CONTROL = 0x3b;
        public byte ADC_CONTROL = 0x30;
        public byte ADC_CONVERT = 0x34;
        public byte IO_CONRTOL = 0x31;
        public byte ALERT_STATUS = 0x20;
        public byte FAULT_STATUS = 0x21;
        public byte CB_TIME = 0x33;
        public byte CB_CTRL = 0x32;
        public byte RESET = 0x3C;

        public byte bat_pack_address = 0x01;
        public byte write_modifier = 0x80;
        public int disbalanceCriticalValue = 0;
        public int cellMaxVoltage = 0;
        public int cellMinVoltage = 0;
        public int autorunProcess = 0;
        public int readElapseTime = 0;
        public string SerialPort = "";
        public string SerialBaud = "";


        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = "config.xml";

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            timer1.Enabled = true;

            SerialPort = config.AppSettings.Settings["SerialPort"].Value;
            SerialBaud = config.AppSettings.Settings["BaudRate"].Value;

            string OwnerInfo = config.AppSettings.Settings["OwnerInfo"].Value;

            disbalanceCriticalValue = Convert.ToInt32(config.AppSettings.Settings["disbalanceCriticalValue"].Value);
            cellMaxVoltage = Convert.ToInt32(config.AppSettings.Settings["cellMaxVoltage"].Value);
            cellMinVoltage = Convert.ToInt32(config.AppSettings.Settings["cellMinVoltage"].Value);
            autorunProcess = Convert.ToInt32(config.AppSettings.Settings["autorun"].Value);
            readElapseTime = Convert.ToInt32(config.AppSettings.Settings["readElapseTime"].Value);

            _port = new SerialPort(SerialPort, int.Parse(SerialBaud));
            _port.ReadBufferSize = 1024;
            //OpenPort();

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

            if (readElapseTime > 100 & readElapseTime < 10000)
            {
                ReadElapseTimer.Interval = readElapseTime;
            }
            else
            {
                MessageBox.Show("Incorrect timer value. Please check configuration and restart aplication" + Environment.NewLine + "The value must be more than 100ms and less than 10000ms" + Environment.NewLine + "Current value - " + readElapseTime.ToString());
                Environment.Exit(0);
            }

            if (autorunProcess == 1)
            {
                main_process();
                TimerEn.Checked = true;
                ReadElapseTimer.Enabled = true;
            }
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



        public void main_process()
        {

            if (!_port.IsOpen)
            {
                if (!OpenPort())
                {
                    MessageBox.Show($"The serial port - {SerialPort} does not exist or busy by another application.\nPlease check it and open application again");
                    Environment.Exit(0);
                }

                Thread.Sleep(50);
                BatIdLabel.Text = "BMS ID: 0x" + bat_pack_address.ToString("X2");
                serialTx(new byte[] { BROADCAST, RESET, 0xA5 }, true);

                serialTx(new byte[] { 0x00, ADDRESS_CONTROL, (byte)(bat_pack_address | write_modifier) }, true);
                serialTx(new byte[] { bat_pack_address, ADC_CONTROL, 0x3d }, true);
                serialTx(new byte[] { bat_pack_address, IO_CONRTOL, 0x03 }, true);
            }
            if (batteryBalancingRequest.Checked)
            {
                serialTx(new byte[] { bat_pack_address, ALERT_STATUS, 0x80 }, true);
                serialTx(new byte[] { bat_pack_address, ALERT_STATUS, 0x00 }, true);
                serialTx(new byte[] { bat_pack_address, ALERT_STATUS, 0x08 }, true);
                serialTx(new byte[] { bat_pack_address, ALERT_STATUS, 0x00 }, true);

                serialTx(new byte[] { bat_pack_address, CB_TIME, 0x02 }, true);
                serialTx(new byte[] { bat_pack_address, CB_CTRL, 0x01 }, true);

                batteryBalancingRequest.Checked = false;
            }

            int[] cells_voltage = { 0, 0, 0, 0, 0, 0 };
            double[] aditional_data = { 0, 0, 0 };                                       //Battery voltage, TS1, TS2 


            byte[] startADC = { 1, 52, 1 };// crc - 88     |  > 3 52 1 23
            byte[] readADC = { 1, 1, 17 };// crc - 88     |  > 3 1 17 223

            byte[] battery_read_data = serialTx(startADC, true);

            Thread.Sleep(5);
            battery_read_data = serialTx(readADC, false);

            if ((battery_read_data[0] == 0x02 && battery_read_data[1] == 0x01 && battery_read_data[2] == 0x11)) //02-01-11
            {

                int[] aditional_data_numbers = { 3, 17, 17 }; //I have some problem in secon TS channe. In this case data from channel 1 shared to ch2
                byte byteIndex = 0;

                foreach (int n in aditional_data_numbers)
                {
                    double tmp_data = 0.0;
                    try
                    {
                        if (n == 3)
                        {
                            tmp_data = Math.Round((battery_read_data[n] * 256 + battery_read_data[n + 1]) * 33.333 / 16383, 3);
                            aditional_data[byteIndex] = Convert.ToInt32(tmp_data * 1000);
                        }
                        else
                        {
                            tmp_data = ((battery_read_data[n] * 256 + battery_read_data[n + 1]) / 8);
                            aditional_data[byteIndex] = Convert.ToInt32(tmp_data - 400);
                        }


                        byteIndex++;

                    }
                    catch
                    {
                        //Need to make error handler
                    }
                }

                int[] numbers = { 5, 7, 9, 11, 13, 15 };
                byte cellIndex = 0;
                foreach (int n in numbers)
                {
                    try
                    {

                        double voltage = Math.Round((battery_read_data[n] * 256 + battery_read_data[n + 1]) * 6.250 / 16383, 3);
                        cells_voltage[cellIndex] = Convert.ToInt32(voltage * 1000);
                        cellIndex++;


                    }
                    catch
                    {
                        //Need to make error handler
                    }

                }
                drav_display(cells_voltage, aditional_data);
            }

        }

        public void drav_display(int[] cells_data, double[] extra_data)
        {
            CellVoltage1.Text = cells_data[0].ToString("D4") + "mV";
            if (cells_data[0] > cellMaxVoltage || cells_data[0] < cellMinVoltage)
            {
                CellVoltage1.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage1.ForeColor = Color.DarkGreen;
            }

            CellVoltage2.Text = cells_data[1].ToString("D4") + "mV";
            if (cells_data[1] > cellMaxVoltage || cells_data[1] < cellMinVoltage)
            {
                CellVoltage2.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage2.ForeColor = Color.DarkGreen;
            }

            CellVoltage3.Text = cells_data[2].ToString("D4") + "mV";
            if (cells_data[2] > cellMaxVoltage || cells_data[2] < cellMinVoltage)
            {
                CellVoltage3.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage3.ForeColor = Color.DarkGreen;
            }


            CellVoltage4.Text = cells_data[3].ToString("D4") + "mV";
            if (cells_data[3] > cellMaxVoltage || cells_data[3] < cellMinVoltage)
            {
                CellVoltage4.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage4.ForeColor = Color.DarkGreen;
            }

            CellVoltage5.Text = cells_data[4].ToString("D4") + "mV";
            if (cells_data[4] > cellMaxVoltage || cells_data[4] < cellMinVoltage)
            {
                CellVoltage5.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage5.ForeColor = Color.DarkGreen;
            }


            CellVoltage6.Text = cells_data[5].ToString("D4") + "mV";
            if (cells_data[5] > cellMaxVoltage || cells_data[5] < cellMinVoltage)
            {
                CellVoltage6.ForeColor = Color.Red;
            }
            else
            {
                CellVoltage6.ForeColor = Color.DarkGreen;
            }

            batFullBox1.Height = batEmptyBox1.Height - PercentCalc(cells_data[0]) * 2;
            batFullBox2.Height = batEmptyBox1.Height - PercentCalc(cells_data[1]) * 2;
            batFullBox3.Height = batEmptyBox1.Height - PercentCalc(cells_data[2]) * 2;
            batFullBox4.Height = batEmptyBox1.Height - PercentCalc(cells_data[3]) * 2;
            batFullBox5.Height = batEmptyBox1.Height - PercentCalc(cells_data[4]) * 2;
            batFullBox6.Height = batEmptyBox1.Height - PercentCalc(cells_data[5]) * 2;

            MaximumVal.Text = "MAX: " + cells_data.Max() + "mV";
            MinimumVal.Text = "MIN: " + cells_data.Min() + "mV";

            DifferenceVal.Text = "DIFF: " + (cells_data.Max() - cells_data.Min()) + "mV";
            if (disbalanceCriticalValue < (cells_data.Max() - cells_data.Min()))
            {
                DifferenceVal.ForeColor = Color.Red;
            }
            else
            {
                DifferenceVal.ForeColor = Color.DarkGreen;
            }

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

        private bool OpenPort()
        {
            try
            {
                _port.Open();

                return true;
            }
            catch (IOException ex)
            {

                return false;
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

        private void TimerEn_CheckedChanged(object sender, EventArgs e)
        {
            ReadElapseTimer.Enabled = TimerEn.Checked;

        }

        private void ReadElapseTimer_Tick(object sender, EventArgs e)
        {
            main_process();
        }
    }
}