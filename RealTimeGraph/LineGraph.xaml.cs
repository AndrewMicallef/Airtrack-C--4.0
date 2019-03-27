//###############################################################################################################################################################################
/* Name: Air Track C# UWP Program
 * Function: Recieve sensor values. Output Values.
 * Inputs: Mbientlab IMU, Arduino Due (PIXY)
 * Description: Tutorial that has been modified. Severely.
 * Last Modified: 28 June 2018
 * Developer: Leonard Lee
 */

///################################################## Libraries ###############################################################################################
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Windows;

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
using Windows.System.Threading;

///################################################## PROGRAM START ###############################################################################################


namespace RealTimeGraph {

    ///---------------------------------------------- OXY PLOT DEFINITION -------------------------------------------------------------------
    ///Define the plot and plot types
    public class MainViewModel
    {
        public const int MAX_DATA_SAMPLES = 960;
        public MainViewModel(){
            MazePosModel = new PlotModel
            { //HEADER TITLE
                Title = "Maze Position",
                IsLegendVisible = true
            };
            MazePosModel.Series.Add(new LineSeries //actual plot
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "Current Position"
            });
            MazePosModel.Series.Add(new LineSeries //maze plot
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = ""
            });
            MazePosModel.Series.Add(new LineSeries //maze plot icon low
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = ""
            });
            MazePosModel.Series.Add(new LineSeries //maze plot icon high
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = ""
            });
            MazePosModel.Axes.Add(new LinearAxis //Y Axis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 400,
                Minimum = 0,
                Maximum = 400,
                Title = "Y Pixels"
            });
            MazePosModel.Axes.Add(new LinearAxis //X Axis
            { 
                IsPanEnabled = false,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = 400,
                Title = "X Pixels"
            });
            //------------------------------------------------
            EulerModel = new PlotModel
            { //HEADER TITLE
                Title = "Euler Angles",
                IsLegendVisible = true
            };            
            EulerModel.Series.Add(new LineSeries
            { //W-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "Heading"
            });
            EulerModel.Axes.Add(new LinearAxis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -0.1,
                AbsoluteMaximum = 360.1,
                Minimum = 0,
                Maximum = 360,
                Title = "Degrees"
            });
            EulerModel.Axes.Add(new LinearAxis
            { //HOW MANY SAMPLES TO KEEP ON SCREEN (960)
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });

        }        
        public PlotModel EulerModel { get; private set; }
        public PlotModel MazePosModel { get; private set; }

    }

    

    ///---------------------------------------------- PAGE STUFFZ -------------------------------------------------------------------
    /// An empty page that can be used on its own or navigated to within a Frame.
       
    public sealed partial class LineGraph : Page
    {
        ///------------------------------------------ PAGE START FUNCTIONS ----------------------------------------------------------
        public LineGraph()
        {
            InitializeComponent();
        }


        ///------------------------------------------ NAVIGATION TO Window -------------------------------------------------------
        protected async override void OnNavigatedTo(NavigationEventArgs e) //To do at Initialsation
        {
            /*  Functions:  1. Open up FolderPicker()
             *              2. Draw X_Maze_plot
             //*              3. Update Plots
             */ 
            base.OnNavigatedTo(e);     

            //Folder location selection
            await PickFolder();
            csv_stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);            

            //Initialise IMU
            await IMU_initialize(e);
            sensorfusion.Stop();

            //await IMU_EulerAddRoute();

            //IMU_Euler_enable();

            //Draw Maze
            //await Draw_X_Outline_plot();

            //Start UI Refresher/Update Timer
            UIPeriodicTimerSetup();

        }


        ///------------------------------------------ UI ACTION BUTTON FUNCTIONS -------------------------------------------------------
                
        private async void back_Click(object sender, RoutedEventArgs e)
        {
            if (!metawear.InMetaBootMode)
            {
                metawear.TearDown();
                await metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }
        
        //========== Section 1 Connection ==========
        private void arduino_initSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            /*  Functions:  1. Establish Connection with Arduino(s)           
             */
            if (arduino_initSwitch.IsOn)
            {
                Serial_InitializeConnection();
            }
            else
            {
                //Destroy Connections

            }
        }

        //========== Section 2 Stream ==========
        private void IMUstreamSwitch_Toggled(object sender, RoutedEventArgs e) {
            /*  Functions:  1. Enable the pathway of Euler Angles
             *              2. Start Streaming of data to computer
             */
            if (IMUstreamSwitch.IsOn)
            {
                IMU_stream_state = true;
                //IMU_Euler_enable();
                sensorfusion.EulerAngles.Start();
                sensorfusion.Start();
            }

            else
            {
                IMU_stream_state = false;
                sensorfusion.Stop();
                sensorfusion.EulerAngles.Stop();
            }
            
        }

        private void arduino_readSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            /*  Functions:  1. Start streaming serial data into the buffer arrays when enabled and process data
             */
            if (arduino_readSwitch.IsOn)
            {
                Read_Serial_data();
                serial_stream_state = true;
            }


            else
            {
                serial_stream_state = false;
            }
        }

        //========== Section 3 Calibrate ==========
        private void zero_set_Click(object sender, RoutedEventArgs e)
        {
            /*  Functions:  1. Set zero points of x, y and euler
             */
            zerox = 77;
            zeroy = 110;
            zeroangle = angle_maze;
            toggle_zero_set_success = true;
        }
        

        //========== Section 4 Output Settings ==========
        private void data_record_en_switch_Toggled(object sender, RoutedEventArgs e)
        {
            /*  Functions:  1. Enable data record. This toggles value within IMU_adddataroute
             */
            if (data_record_en_switch.IsOn)
            {
                data_save_en = true;
            }

            else
            {
                data_save_en = false;
            }
        }

        private void PIXY_plot_hist_en_switch_Toggled(object sender, RoutedEventArgs e)
        {
            /*  Functions:  1. Enable data record. This toggles value of showing history on screen. Turning off should clear the on screen
             */
            //if (PIXY_plot_hist_switch.IsOn)
            //{
            //    plot_mode_hist_en = true;
            //    toggle_plot_hist_set_success = true;
            //}

            //else
            //{
            //    if(plot_mode_hist_en == true)
            //    {
            //        //Clear the graph
            //        //oxyplot_clear();
            //    }
            //    plot_mode_hist_en = false;
            //}

        }

        private void IMU_plot_en_switch_Toggled(object sender, RoutedEventArgs e)
        {
            if (IMU_plot_en_switch.IsOn)
            {
                IMU_graph_en_state = true;
                toggle_IMU_plot_en_success = true;
            }

            else
            {
                IMU_graph_en_state = false;
            }
        }

        //private void PIXY_plot_en_switch_Toggled(object sender, RoutedEventArgs e)
        //{
        //    if (PIXY_plot_en_switch.IsOn)
        //    {
        //        PIXY_plot_en_state = true;
        //        toggle_PIXY_plot_en_success = true;
        //    }

        //    else
        //    {
        //        PIXY_plot_en_state = false;                
        //    }
        //}

        //========== Section 5 Trial Start/Stop ==========
        private void trial_start_btn_Click(object sender, RoutedEventArgs e)
        {
            trial_active = true;
            Trial_start();
        }

        private void trial_stop_btn_Click(object sender, RoutedEventArgs e)
        {
            trial_active = false;
            Trial_stop();
        }

        private void plot_clear_Click(object sender, RoutedEventArgs e)
        {
            oxyplot_clear();
        }

        private void plot_save_click(object sender, RoutedEventArgs e)
        {
            save_oxyplot();
        }

        //========== Section xxx Trial LED Control ==========
        private void btn_LED_off_Click(object sender, RoutedEventArgs e)
        {
            IMU_NeoPixel(0);
        }

        private void btn_LED_green_Click(object sender, RoutedEventArgs e)
        {
            IMU_NeoPixel(1);
        }

        private void btn_LED_blue_Click(object sender, RoutedEventArgs e)
        {
            IMU_NeoPixel(2);
        }

        private void btn_LED_all_rand_Click(object sender, RoutedEventArgs e)
        {
            IMU_NeoPixel(3);
        }

        private void btn_LED_2x_rand_Click(object sender, RoutedEventArgs e)
        {
            IMU_NeoPixel(4);
        }



        //========== Section xxx Trial Reward Control ==========
        private void btn_Reward_Click(object sender, RoutedEventArgs e)
        {

        }




        //========== Section xxx Trial Status Control ==========





    }
}










































/** --  Junk Code--

    Original Code

metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
accelerometer = metawear.GetModule<IAccelerometer>();
accelerometer.Configure(odr: 100f, range: 8f);


     //var csv = new StringBuilder(data_entry);
            //csv.AppendLine(Environment.NewLine);
            //string pathString = System.IO.Path.Combine(@"C:\airtrack_csv", "data.csv");
            // File.AppendAllText("data.csv", csv.ToString());

            //await Windows.Storage.FileIO.WriteTextAsync(MyFile, data_entry + Environment.NewLine);
            //await Windows.Storage.FileIO.WriteTextAsync(usedFile, Enviro);

            //using (IRandomAccessStream textStream = await MyFile.OpenAsync(FileAccessMode.ReadWrite))
            //{
            //    using (DataWriter textWriter = new DataWriter(textStream))
            //    {
            //        textWriter.WriteString(data_entry + Environment.NewLine);
            //        await textWriter.StoreAsync();
            //    }
            //}

            //var stream = await MyFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            //using (var outputStream = stream.GetOutputStreamAt(stream.Size))
            //{
            //    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
            //    {
            //        dataWriter.WriteString(data_raw + "\r\n" );
            //        await dataWriter.StoreAsync();
            //        await outputStream.FlushAsync();
            //    }
            //}
            // stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.

            //System.IO.File.AppendAllText(StorageFile MyFile, "text content");

            //await Windows.Storage.FileIO.AppendTextAsync(MyFile, data_raw);



     _readWriteLock.EnterWriteLock();
            try
            {
                using (var outputStream = stream.GetOutputStreamAt(stream.Size))
                {
                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                    {
                        dataWriter.WriteString(data_q_ts + "," + data_q_W + "," + data_q_Z + "\r\n");
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
                stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.                
            }

            finally
            {
                _readWriteLock.ExitWriteLock();
            }



             //settings.OnDisconnectAsync(() => {
            //    ILed led = metawear.GetModule<ILed>();
            //    led.EditPattern(Color.Red, Pattern.Solid, count: 2);
            //    led.Play();
            //});

            //MbientLab.MetaWear.IMetaWearBoard.ReadBatteryLevelAsync()

            //====== Creating .CSV Folder for data to be stored           
            //StorageFolder newFolder = await DownloadsFolder.CreateFolderAsync("--Mbientlab Data-- ");
            //StorageFile newFile = await DownloadsFolder.CreateFileAsync("test.txt");

            //StorageFolder MyFolder = await DownloadsFolder.CreateFolderAsync("-- AIRTRACK IMU Data --", CreationCollisionOption.OpenIfExists);
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data.csv", CreationCollisionOption.GenerateUniqueName);

            //StorageFolder MyFolder = await DownloadsFolder.CreateFolderAsync("AIRTRACK_IMU_Data_2", CreationCollisionOption.OpenIfExists);
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data.csv", CreationCollisionOption.OpenIfExists);
            
            //-----------------------

            //await sensorfusion.LinearAcceleration.AddRouteAsync(source => source.Stream(async data =>            
            //{                
            //    var value = data.Value<Acceleration>();
            //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //    {                    
            //        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.X));
            //        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Y));
            //        (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Z));
            //        samples++;  
            //        model.InvalidatePlot(true);
            //        if (samples > MainViewModel.MAX_DATA_SAMPLES)
            //        {
            //            model.Axes[1].Reset();
            //            model.Axes[1].Maximum = samples;
            //            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
            //            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
            //        }
            //    });
            //}));




    // Set Status to Locked           
    ISSUE WITH THIS IS OPENING AND WRITING AND CLOSING IS RESOURCE HEAVY. NEED TO STREAM. 
            List<string> data_line = new List<string>() { data_q_ts + "," + data_q_W + "," + data_q_Z };
            FileIO.AppendLinesAsync(file, data_line);     




    //string path = @"C:\Users\lab-admin\AppData\Local\Packages\fcbddcdb-d674-485d-b099-9550c89131c5_wkrz0q4h1x9v2\Movement_data_2.csv";

            //var myFile = File.Create(path);
            //myFile.Close();

            //Use stream to write to the file
            //StorageFolder MyFolder = ApplicationData.Current.LocalFolder; //await DownloadsFolder.CreateFolderAsync("AIRTRACK_IMU_Data_2");
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data_2.csv", CreationCollisionOption.OpenIfExists);
            //var stream = await MyFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite, StorageOpenOptions.AllowReadersAndWriters);

            // Set Status to Locked            
            //List<string> data_line = new List<string>() { data_q_ts + "," + data_q_W + "," + data_q_Z };
            //FileIO.AppendLinesAsync(file, data_line);

            //var stream = file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            //using (var outputStream = stream.GetOutputStreamAt(stream.Size))
            //{
            //    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
            //    {
            //        dataWriter.WriteString(data_raw + "\r\n");
            //        dataWriter.StoreAsync();
            //        outputStream.FlushAsync();
            //    }
            //}

            
                               
    
    
    
    //readData(data);
                    
                    //stopwatch.Stop();

                    //Running Average
                    //rate_cnt_5 = rate_cnt_4;
                    //rate_cnt_4 = rate_cnt_3;
                    //rate_cnt_3 = rate_cnt_2;
                    //rate_cnt_2 = rate_cnt_1;
                    //rate_cnt_1 = (int)stopwatch.ElapsedMilliseconds;
                    //rate_cnt_avg = (rate_cnt_1 + rate_cnt_2 + rate_cnt_3 + rate_cnt_4 + rate_cnt_5) / 5;

                    //if (rate_cnt_avg == 0)
                    //{
                    //    rate_cnt_hz = 0;
                    //}
                    //else if (rate_cnt_avg > 0)
                    //{
                    //    rate_cnt_hz = 1000 / rate_cnt_avg;
                    //}




    //X Y DEGREE Processing

    //if (x_disp < 1)
            //{
            //    if (x_neg_dir == false)
            //    {
            //        //oxyplot_x = 200 + x_disp;
            //    }

            //    else if (x_neg_dir == true)
            //    {
            //        //oxyplot_x = 200 - x_disp;
            //    }

            //    if (y_neg_dir == false)
            //    {
            //        //oxyplot_y = 200 + y_disp;
            //    }

            //    else if (y_neg_dir == true)
            //    {
            //        //oxyplot_y = 200 - y_disp;
            //    }
            //}

            //else
            //{
            //    if ((angle_actual_maze >= 0 && angle_actual_maze <= 30) || (angle_actual_maze >= 330 && angle_actual_maze <= 360))
            //    {       
            //        oxyplot_x = 200 - x_disp;
            //        oxyplot_y = 200 - y_disp;
            //    }

            //    else if (angle_actual_maze >= 60 && angle_actual_maze <= 120)
            //    {
            //        oxyplot_x = 200 - y_disp;
            //        oxyplot_y = 200 + x_disp;
            //    }

            //    else if (angle_actual_maze >= 150 && angle_actual_maze <= 210)
            //    {
            //        oxyplot_x = 200 + x_disp;
            //        oxyplot_y = 200 + y_disp;
            //    }

            //    else if (angle_actual_maze >= 240 && angle_actual_maze <= 300)
            //    {
            //        oxyplot_x = 200 + y_disp;
            //        oxyplot_y = 200 - x_disp;
            //    }                
            //}


            /* Proper way that doesn't work that involves caluclating each thing individually

            radius = (int)Math.Sqrt((x_disp * x_disp) + (y_disp * y_disp));
           

            if(y_disp != 0 && x_disp != 0)
            {
                angle = (float)Math.Tanh(y_disp / x_disp);
            }
            
            if (y_disp < 0)
            {
                if (angle_actual_maze + angle > 360)
                {
                    angle2 = angle_actual_maze + angle - 360;
                }

                else
                {
                    angle2 = angle_actual_maze + angle;
                }
            }

            else
            {
                if (angle_actual_maze - angle < 0)
                {
                    angle2 = 360 - angle - angle_actual_maze;
                }

                else
                {
                    angle2 = angle_actual_maze - angle;
                }
            }

            x_post = (radius * Math.Cos(angle2));
            y_post = (radius * Math.Sin(angle2));

            oxyplot_x = 200 + x_post;
            oxyplot_y = 200 + y_post;


            










  **/
