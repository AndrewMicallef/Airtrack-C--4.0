using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using MbientLab.MetaWear.Core.Settings;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO.Ports;


using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Streams;
using MbientLab.MetaWear.Peripheral.NeoPixel;

namespace RealTimeGraph
{
    partial class LineGraph
    {
        ///------------------------------------------ Arduino Uno PIXY Initialization FUNCTION -----------------------------------------
        public async Task Serial_InitializeConnection()
        {
            try
            {
                LineGraph content = Frame.Content as LineGraph;
                var aqs_uno = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x2341, 0x0043); //0x2A03, 0x0057 (Uno On Table = 0x2341, 0x0043) (Due Programming: 0x2341, 0x003D) (Due Native USB: 0x2341, 0x003E)
                var info_uno = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs_uno);

                var aqs_due = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x2341, 0x003D); //0x2A03, 0x0057 (Uno On Table = 0x2341, 0x0043) (Due Programming: 0x2341, 0x003D) (Due Native USB: 0x2341, 0x003E UWP UNSUPPORTED)
                var info_due = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs_due);

                // Get connection data FOR aRDUINO uNO
                for (int i = 0; i != info_uno.Count; i++)
                {
                    if (info_uno[i].Name.Contains("COM12")) //Reading
                    {
                        serialPortUnoPixy = await SerialDevice.FromIdAsync(info_uno[i].Id);
                        // Configure serial settings
                        serialPortUnoPixy.DataBits = 8;
                        serialPortUnoPixy.BaudRate = 115200;
                        serialPortUnoPixy.Parity = SerialParity.None;
                        serialPortUnoPixy.Handshake = SerialHandshake.None;
                        serialPortUnoPixy.StopBits = SerialStopBitCount.One;
                        serialPortUnoPixy.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                        serialPortUnoPixy.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                        UnoPixydataReader = new DataReader(serialPortUnoPixy.InputStream);

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            content.program_log.Text = "Connected to Arduino Uno COM12 PIXY \r\n" + content.program_log.Text;
                        });
                    }

                    if (info_uno[i].Name.Contains("COM5")) //Writing to
                    {                       
                        //
                        
                    }
                   

                    func_serial_init_success = true;
                }

                // Get connection data FOR aRDUINO Due
                serialPortDueReward_Write = await SerialDevice.FromIdAsync(info_due[0].Id);
                // Configure serial settings
                serialPortDueReward_Write.DataBits = 8;
                serialPortDueReward_Write.BaudRate = 115200;
                serialPortDueReward_Write.Parity = SerialParity.None;
                serialPortDueReward_Write.Handshake = SerialHandshake.None;
                serialPortDueReward_Write.StopBits = SerialStopBitCount.One;
                serialPortDueReward_Write.ReadTimeout = TimeSpan.FromMilliseconds(10);
                serialPortDueReward_Write.WriteTimeout = TimeSpan.FromMilliseconds(10);

                serialPortDueReward_Write.IsDataTerminalReadyEnabled = true;

                DueRewarddataWriter = new DataWriter(serialPortDueReward_Write.OutputStream);                

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    content.program_log.Text = "Connected to Arduino DUE REWARD Write \r\n" + content.program_log.Text;
                });
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Serial Init error" + ex.Message);
            }
        }


        ///------------------------------------------ Arduino Uno PIXY Read data FUNCTION ----------------------------------------------
        private async Task PixyReadAsync()
        {
            /* Active Method:
                 * 1. Read array1 1 byte
                 * 2. Read value
                 * 2a. If number, write to array2
                 * 2b. If " ", array3 = array2. Clear array2
                 * 2c. If " ", array4 = array2. Clear array2
                 */

            try
            {
                Task<uint> loadAsyncTask;
                UnoPixydataReader.ByteOrder = ByteOrder.BigEndian;
                UnoPixydataReader.InputStreamOptions = InputStreamOptions.ReadAhead;
                UnoPixydataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                uint readBufferLength = 1;

                loadAsyncTask = UnoPixydataReader.LoadAsync(readBufferLength).AsTask();
                uint ReadAsyncBytes = await loadAsyncTask;

                if (ReadAsyncBytes > 0)
                {
                    string data = UnoPixydataReader.ReadString(readBufferLength);
                    //System.Diagnostics.Debug.Write(data);
                    array1 = data;
                }

                if (array1[0] == (' '))
                {
                    array3 = array2;
                    array2 = ""; //
                }

                else if (array1[0] == ('\r'))
                {
                    array4 = array2;
                    array2 = ""; //<-- write array3 function here to the graph
                    oxyplot_MazePosModel();
                }

                else if (array1[0] == ('\n'))
                {
                    //do nothing
                }

                else
                {
                    array2 += array1[0];
                }

                datax_box.Text = array3;
                datay_box.Text = array4;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Pixy Read error" + ex.Message);
            }

            if(func_pixy_read_already_trig == false)
            {
                func_pixy_read_success = true;
                func_pixy_read_already_trig = true;
            }
            
        }



        ///------------------------------------------ Arduino Uno REWARD Write data FUNCTION ----------------------------------------------
        private async Task RewardWriteAsync(string value)
        {
            try
            {
                DueRewarddataWriter.WriteString(value);
                await DueRewarddataWriter.StoreAsync();
                //await UnoRewarddataWriter.FlushAsync();
                //UnoRewarddataWriter.DetachStream();
                //UnoRewarddataWriter = null;
                
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Reward write error" + ex.Message);
            }
        }



        ///------------------------------------------ Arduino Uno Reward Read data FUNCTION ----------------------------------------------
        private async Task RewardReadAsync()
        {
            /* Active Method:
                 * 1. Read array1 1 byte
                 * 2. Read value
                 * 2a. If number, write to array2
                 * 2b. If " ", array3 = array2. Clear array2
                 * 2c. If " ", array4 = array2. Clear array2
                 */

            try
            {
                Task<uint> loadAsyncTask;
                UnoRewarddataReader.ByteOrder = ByteOrder.BigEndian;
                UnoRewarddataReader.InputStreamOptions = InputStreamOptions.ReadAhead;
                UnoRewarddataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                uint readBufferLength = 1;

                loadAsyncTask = UnoRewarddataReader.LoadAsync(readBufferLength).AsTask();
                uint ReadAsyncBytes = await loadAsyncTask;

                if (ReadAsyncBytes > 0)
                {
                    string data = UnoRewarddataReader.ReadString(readBufferLength);
                    //System.Diagnostics.Debug.Write(data);
                    serial_reward_buf1 = data;
                }

                if (serial_reward_buf1[0] == (' ')) //if space
                {
                    //do nothing
                }

                else if (serial_reward_buf1[0] == ('\r')) //if return
                {
                    //do nothing                   
                }

                else if (serial_reward_buf1[0] == ('\n')) //if new line
                {
                    serial_reward_outputbuf = serial_reward_buf2;
                    serial_reward_buf2 = "";
                }

                else //else if everything else, write into buffer
                {
                    serial_reward_buf2 += serial_reward_buf1[0];
                }                
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("reward read error" + ex.Message);
            }            

        }


        public async Task<IScheduledTask> progranScheduleRead()
        {            
            return await metawear.ScheduleAsync(4000, false, () => metawear.GetModule<IHaptic>().StartMotor(5000, 80));
        }



        ///------------------------------------------ IMU Initialise FUNCTION ----------------------------------------------------------
        private async Task IMU_initialize(NavigationEventArgs e)
        {
            try
            {
                // Sensor Configuration
                metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);

                //ILed led = metawear.GetModule<ILed>();
                //led.EditPattern(Color.Green, duration: 500, highTime: 250, high: 16, low: 0, count: 50);
                //led.Play();

                // ## Reset Neopixel to Off (if on before)
                INeoPixel neopixel = metawear.GetModule<INeoPixel>();

                IStrand strand = neopixel.InitializeStrand(1, ColorOrdering.WS2811_GRB, StrandSpeed.Slow, 0, 3);
                strand = neopixel.LookupStrand(1);

                strand.Hold();
                strand.SetRgb(0, 0, 0, 0);
                strand.SetRgb(1, 0, 0, 0);
                strand.SetRgb(2, 0, 0, 0);
                strand.Release();

                //strand.Hold();
                //strand.SetRgb(0, 20, 0, 0);
                //strand.SetRgb(1, 0, 20, 0);
                //strand.SetRgb(2, 0, 0, 20);
                //strand.Release();

                //strand.Rotate(RotationDirection.Away, 500);               

                //await progranScheduleRead();

                //IScheduledTask mwTask;
                //if ((mwTask = metawear.LookupScheduledTask((byte)0)) != null)
                //{
                //    // start the task
                //    mwTask.Start();

                //    // stop the task
                //    mwTask.Stop();

                //    // remove the task, id 0 no longer valid id
                //    mwTask.Remove();
                //}               


                //// Run a motor for 1000ms at 50% strength
                //metawear.GetModule<IHaptic>().StartMotor(1000, 100f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 95f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 90f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 85f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 80f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 75f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 70f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 65f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 60f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 55f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 50f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 45f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 40f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 35f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 30f);
                //metawear.GetModule<IHaptic>().StartMotor(1000, 25f);

                //// turn off leds from index [0, 59] (60 leds)
                //strand.Clear(0, 59);

                //// free
                //strand.Free();


                sensorfusion = metawear.GetModule<ISensorFusionBosch>();
                sensorfusion.Configure(mode: Mode.Ndof, ar: AccRange._2g, gr: GyroRange._500dps);

                ISettings settings = metawear.GetModule<ISettings>();
                settings.EditBleConnParams(maxConnInterval: 7.5f);

                await settings.Battery.AddRouteAsync(source => source.Stream(data => Console.WriteLine("battery = " + data.Value<BatteryState>())));
                settings.Battery.Read();

                IMacro macro = metawear.GetModule<IMacro>();

                func_IMU_init_success = true;


                await sensorfusion.EulerAngles.AddRouteAsync(source => source.Stream(async data =>
                {
                    var value = data.Value<EulerAngles>();
                    imu_data = data;

                    //set time_base to false to reset time_base
                    if (time_base_set == false)
                    {
                        time_base_ms = data.Timestamp.Millisecond;
                        time_base_sec = data.Timestamp.Second;
                        time_base_min = data.Timestamp.Minute;
                        time_base_hour = data.Timestamp.Hour;
                        base_elapsed_millisec = time_base_ms + (1000 * time_base_sec) + (1000 * 60 * time_base_min) + (1000 * 60 * 60 * time_base_hour);
                        time_base_set = true;
                    }

                    //Calculate elapsed time
                    time_current_ms = data.Timestamp.Millisecond;
                    time_current_sec = data.Timestamp.Second;
                    time_current_min = data.Timestamp.Minute;
                    time_current_hour = data.Timestamp.Hour;
                    current_elapsed_millisec = time_current_ms + (1000 * time_current_sec) + (1000 * 60 * time_current_min) + (1000 * 60 * 60 * time_current_hour);
                    trial_time_ms = current_elapsed_millisec - base_elapsed_millisec;

                    angle_maze = value.Heading;

                    if (data_save_en == true & trial_active == true)
                    {
                        saveToCSV(data, false); //Start this
                    }

                    if (func_IMU_euler_init_already_trig == false)
                    {
                        func_IMU_euler_init_success = true;
                        func_IMU_euler_init_already_trig = true;
                    }
                }));
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("IMU init error" + ex.Message);
            }

            
        }
        




        ///------------------------------------------ IMU Euler AddRoute FUNCTION ----------------------------------------------------------
        private async Task IMU_EulerAddRoute() //bad function?
        {
            try
            {
                LineGraph content = Frame.Content as LineGraph;
                //var model1 = (DataContext as MainViewModel).EulerModel;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("IMU euler data route error" + ex.Message);
            }
        }





        ///------------------------------------------ IMU Euler Enable Function ----------------------------------------------------------
        public async void IMU_Euler_enable()
        {
            if(IMU_stream_state == true)
            {
                await IMU_EulerAddRoute();                
            }
        }






        ///------------------------------------------ UI Based Arduino Read Enable Function ---------------------------------------------
        public async Task Read_Serial_data()
        {
            try
            {
                while (arduino_readSwitch.IsOn)
                {
                    await PixyReadAsync();
                    //await oxyplot_MazePosModel();
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("read serial data error" + ex.Message);
            }

        }


        


        ///------------------------------------------ Process PIXY Data into coordinates ---------------------------------------------------------
        public async Task oxyplot_MazePosModel()
        {
            /* Input variables:
             * array3 = x string
             * array4 = y string
             * angle_maze = raw maze angle
             * zerox
             * zeroy
             * zeroangle
             * angle_actual_maze = actual angle after calibration
             */

            //Calibrate the rotation of the IMU sensor
            //measured(150) - zero point(30), actual = 120. If measured >= zero point, actual = measured - zero point
            //If measured (10) - zero (30). Else if measured < zero point, 360 - (zero point - measured)
            //If value is less than 360, thats fine
            //if value is more than 720,

            
            // =============== Gather PIXY Raw Coordinates
            var model2 = (DataContext as MainViewModel).MazePosModel;
            Int32.TryParse(array3, out x_coor);
            Int32.TryParse(array4, out y_coor);

            int X = x_coor - zerox;
            int Y = y_coor - zeroy;
            
            // this would be easier in radians...
            angle_maze = (float) (DegreeToRadian((double) angle_maze) - DegreeToRadian((double) zeroangle));
            var theta = Math.Atan2(Y, X) + DegreeToRadian(angle_maze);

            


            // =============== Calculate oxy_plot X and Y depending on angle
            if ((angle_actual_maze >= 0 && angle_actual_maze <= 30) || (angle_actual_maze >= 330 && angle_actual_maze <= 360))
            {
                processed_x = 200 - x_disp;
                processed_y = 200 - y_disp;
                trial_maze_dir = 0;
            }

            else if (angle_actual_maze >= 90 && angle_actual_maze <= 150)
            {
                processed_x = 200 + y_disp;
                processed_y = 200 - x_disp;
                trial_maze_dir = 1;
            }

            else if (angle_actual_maze >= 210 && angle_actual_maze <= 270)
            {
                processed_x = 200 + x_disp;
                processed_y = 200 + y_disp;
                trial_maze_dir = 2;
            }
            

            //// =============== Calculate if previous values were the same as to not write them again
            //if (buffer_x == processed_x && buffer_y == processed_y)
            //{
            //    x_y_plot_same = true;
            //}

            //else
            //{
            //    x_y_plot_same = false;
            //}

            // =============== Automatic Reward Function ===================
            //await auto_reward();
            

            //// =============== Values will not be written if:
            //if (PIXY_plot_en_state == true && PIXY_graph_clear_state == false && x_y_plot_same == false && processed_x > 0 && processed_y > 0)
            //{
            //    if (((processed_x >= 200 - arm_width / 2 && processed_x <= 200 + arm_width / 2) && //A
            //    (processed_y >= 200 - arm_width / 2 && processed_y <= 200 + arm_width / 2)) ||

            //    ((processed_x >= 200 - arm_length && processed_x <= 200 + arm_width / 2) && //B
            //    (processed_y >= 200 - arm_width / 2 && processed_y <= 200 + arm_width / 2)) ||

            //    ((processed_x >= 200 - arm_width / 2 && processed_x <= 200 + arm_width / 2) && //C
            //    (processed_y >= 200 + arm_width / 2 && processed_y <= 200 + arm_length)) ||

            //    ((processed_x >= 200 + arm_width / 2 && processed_x <= 200 + arm_length) && //D
            //    (processed_y >= 200 - arm_width / 2 && processed_y <= 200 + arm_width / 2)) ||

            //    ((processed_x >= 200 - arm_width / 2 && processed_x <= 200 + arm_width / 2) && //E
            //    (processed_y >= 200 - arm_length && processed_y <= 200 - arm_width / 2)))
            //    {
            //        oxyplot_x = processed_x;
            //        oxyplot_y = processed_y;
            //        oxyplot_within_boundaries = true;
            //    }

            //    else oxyplot_within_boundaries = false;
            //}
                
        }

        
        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public async Task IMU_NeoPixel(int val)        {
            
            INeoPixel neopixel = metawear.GetModule<INeoPixel>();

            IStrand strand = neopixel.InitializeStrand(1, ColorOrdering.WS2811_GRB, StrandSpeed.Slow, 0, 3);
            strand = neopixel.LookupStrand(1);


            if (val == 0)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 0, 0);
                strand.SetRgb(1, 0, 0, 0);
                strand.SetRgb(2, 0, 0, 0);
                strand.Release();
            }

            else if (val == 1)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 20, 0);
                strand.SetRgb(1, 0, 20, 0);
                strand.SetRgb(2, 0, 20, 0);
                strand.Release();
            }

            else if(val == 2)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 0, 20);
                strand.SetRgb(1, 0, 0, 20);
                strand.SetRgb(2, 0, 0, 20);
                strand.Release();
            }

            else if (val == 3)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 0, 0);
                strand.SetRgb(1, 0, 0, 0);
                strand.SetRgb(2, 0, 0, 0);
                strand.Release();
            }

            else if (val == 4)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 0, 0);
                strand.SetRgb(1, 0, 0, 0);
                strand.SetRgb(2, 0, 0, 0);
                strand.Release();
            }

            else if (val == 5)
            {
                strand.Hold();
                strand.SetRgb(0, 0, 0, 0);
                strand.SetRgb(1, 0, 0, 0);
                strand.SetRgb(2, 0, 0, 0);
                strand.Release();
            }


        }














    }
}


/*
 
    coordinates [];

    Public void Update_coordinates(Xpos, Ypos, raw_angle) 
    {
        
        float radius = Math.Sqrt(Math.Pow(Xpos, 2) + Math.pow(Ypos, 2));
        float theta = Math.Atan2(Ypos, Xpos) + raw_angle;

        float mx = Math.Cos(theta) * radius;
        float my = Math.Sin(theta) * radius;

        coordinates[0] = mx;
        coordinates[1] = my;
        
        
    }



    ///

    Read the values of x,y, angle

    update our coordinate matrix

    write our 
    ///
 */
