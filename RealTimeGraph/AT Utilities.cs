using OxyPlot;

using MbientLab.MetaWear;
using MbientLab.MetaWear.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Storage;

namespace RealTimeGraph
{
    partial class LineGraph
    {
        ///------------------------------------------ SAVE DATA TO STREAM -------------------------------------------------------------
        public async Task saveToCSV(IData data, bool stop)
        {
            try
            {
                /* Ways to reduce:
                 * Do not write any values that are repeating over the previous one
                 * 
                 */
                
                var data_quaternion = data.Value<EulerAngles>();
                var data_q_ts = data.FormattedTimestamp;
                var data_q_H = data_quaternion.Heading;

                string1 = data_q_ts + "," + trial_time_ms.ToString() + "," + data_q_H + "," + array3 + "," + array4 + "," + angle_actual_maze + "," + processed_x + "," + processed_y + "\r\n";

                if (stop == false)
                {
                    if (i < 1)
                    {
                        builder.Append(string1);
                        i++;
                    }

                    else
                    {
                        builder.Append(string1);
                        string innerString = builder.ToString();
                        using (var outputStream = csv_stream.GetOutputStreamAt(csv_stream.Size))
                        {
                            using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                            {
                                dataWriter.WriteString(innerString);
                                await dataWriter.StoreAsync();
                                await outputStream.FlushAsync();
                            }
                        }

                        innerString = ""; //flush varaible
                        builder.Clear();
                        i = 0; //reset counter
                    }
                }

                if(stop == true)
                {
                    builder.Append(string1);
                    string innerString = builder.ToString();
                    using (var outputStream = csv_stream.GetOutputStreamAt(csv_stream.Size))
                    {
                        using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                        {
                            dataWriter.WriteString(innerString);
                            await dataWriter.StoreAsync();
                            await outputStream.FlushAsync();
                        }
                    }

                    innerString = ""; //flush varaible
                    builder.Clear();
                    i = 0; //reset counter
                }       
                

                if (toggle_data_rec_set_already_trig == false)
                {
                    toggle_data_rec_set_success = true;
                    toggle_data_rec_set_already_trig = true;
                }



            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("save to csv error" + ex.Message);
            }
        }





        
        ///------------------------------------------ READ DATA FROM IMU FUNCTIONS -------------------------------------------------------
        public async Task readData(IData data)
        {
            var buffer = data.Value<EulerAngles>();
            System.Diagnostics.Debug.WriteLine(buffer);
        }





        
        ///------------------------------------------ UI FOLDER SELECTION FUNCTION -------------------------------------------------------
        public async Task PickFolder()
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add(".csv");
                folderPicker.FileTypeFilter.Add(".txt");

                while (folder == null)
                {
                    folder = await folderPicker.PickSingleFolderAsync();
                }

                if (folder != null)
                {
                    // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                    //string current_time = DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss");
                    string current_time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    file_name = "IMU_Sensor_" + current_time + ".csv";
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                    mainFolder = await folder.CreateFolderAsync("Movement_Data", CreationCollisionOption.OpenIfExists);
                    file = await mainFolder.CreateFileAsync(file_name, CreationCollisionOption.GenerateUniqueName);
                    List<string> lines = new List<string>() { "TimeStamps ,Elapsed Time ,RAW IMU Heading ,RAW PIXY X ,RAW PIXY Y ,Calibrated Angle ,Maze X ,Maze Y" };
                    await FileIO.WriteLinesAsync(file, lines);

                    func_folder_made_success = true;
                }

                else
                {
                    // the user didn't select any folder
                }                
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("pick folder error" + ex.Message);
            }
        }





        ///------------------------------------------ OXYPLOT save pdf FUNCTION -------------------------------------------------------
        public async Task save_oxyplot()
        {
            try
            {
                var model2 = (DataContext as MainViewModel).MazePosModel;
                string current_time = DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss");
                string plot_save_name = "OxyPlot_" + current_time + ".pdf";

                var oxy_file = await mainFolder.CreateFileAsync(plot_save_name, CreationCollisionOption.GenerateUniqueName);                
                var oxy_outputStream = await oxy_file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);  

                using (Stream stream = oxy_outputStream.AsStream())
                {
                    var pdfExporter = new PdfExporter { Width = 600, Height = 600 };
                    pdfExporter.Export(model2, stream);                    
                }

                func_plot_saved_success = true;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save Oxyplot error" + ex.Message);
            }

        }





        ///------------------------------------------ UI Update Timer Init FUNCTION -------------------------------------------------------
        public void UIPeriodicTimerSetup()
        {
            //10Hz
            dispatcherTimer10Hz = new DispatcherTimer();
            dispatcherTimer10Hz.Tick += UIdispatcherTimer_Tick10Hz;
            dispatcherTimer10Hz.Interval = new TimeSpan(0,0,0,0,100);            
            startTime10Hz = DateTimeOffset.Now;
            lastTime10Hz = startTime10Hz;            
            dispatcherTimer10Hz.Start();

            //50Hz This says 100Hz but its actually just 20Hz
            dispatcherTimer100Hz = new DispatcherTimer();
            dispatcherTimer100Hz.Tick += UIdispatcherTimer_Tick100Hz;
            dispatcherTimer100Hz.Interval = new TimeSpan(0, 0, 0, 0, 100);            
            startTime100Hz = DateTimeOffset.Now;
            lastTime100Hz = startTime100Hz;            
            dispatcherTimer100Hz.Start();
        }





        ///------------------------------------------ UI Update 10Hz FUNCTION -------------------------------------------------------
        void UIdispatcherTimer_Tick10Hz(object sender, object e)
        {
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime10Hz;
            lastTime10Hz = time;            
            UI_textblock_update();
            timesTicked10Hz++;            
        }

        
        
        
        ///------------------------------------------ IMU euler Update 100Hz FUNCTION -------------------------------------------------------
        void UIdispatcherTimer_Tick100Hz(object sender, object e)
        {
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime10Hz;
            lastTime100Hz = time;
            //IMU_Euler_enable();
            timesTicked100Hz++;

            if (auto_reward_en == true && trial_active == true)
            {
                auto_reward();
            }

        }






        ///------------------------------------------ Trial Start Function -------------------------------------------------------
        public void Trial_start()
        {
            /* Functions:   1. Reset and Start Timer
             *              2. Enable the save of data to csv -- trial_active
             *              3. Enable Plot writing -- trial_active
             *              4. Change box color
             *              5. Write to program log
             */

            trial_timer.Reset();    //Reset Timer
            trial_timer.Start();    //Start Timer
            time_base_set = false;  //Reset point
        }






        ///------------------------------------------ Trial Stop Function -------------------------------------------------------
        public void Trial_stop()
        {
            /* Functions:   1. Stop Timer
             *              2. Disable the save of data to csv
             *              3. Disable Plot writing
             *              4. Trial = Inactive
             *              5. Write to program log
             */
            trial_timer.Stop();
            saveToCSV(imu_data, true);
        }






        ///------------------------------------------ Trial Stop Function -------------------------------------------------------
        public async Task auto_reward()
        {

            string arduino_data_string = x_disp.ToString() + "," + y_disp.ToString() + "," + trial_maze_dir.ToString() + '\n';   
            await RewardWriteAsync(arduino_data_string);            

        }
    }
}




//await RewardReadAsync();

////valid Arm of Maze
//if (trial_maze_dir != trial_maze_dir_of_last_reward)
//{
//    valid_reward_arm = true;
//}

////Valid Reward Position in Maze
//if (x_disp > min_x_disp && y_disp < max_y_disp && y_disp > min_y_disp)
//{
//    valid_reward_loc = true;
//}

//else
//{
//    valid_reward_loc = false;
//}

//switch (reward_state)
//{
//    case 0: //Idle
//        //If    'rc' read, if valid_arm true, if valid_pos true -> write "e" & go to state 1
//        //Else  Nothing
//        if(valid_reward_arm == true && valid_reward_loc == true)
//        {
//            await RewardWriteAsync("e");
//            reward_state = 1;
//        }
//        break;

//    case 1: //Extending
//        //If    "ec" read -> go to state 3
//        //Else  if valid_arm false, if valid_pos false -> write "r" and go to state 2
//        if(serial_reward_outputbuf == "ec" && valid_reward_arm == true && valid_reward_loc == true)
//        {
//            reward_state = 3;
//        }

//        else if(valid_reward_arm == false || valid_reward_loc == false)
//        {
//            await RewardWriteAsync("r");
//            reward_state = 2;
//        }
//        break;

//    case 2: //Emergency Retract
//        //If    valid_pos == true -> write "e" and go to state 1
//        //Else  if "rc" read, go to state 1
//        if(valid_reward_loc == true && valid_reward_arm == true)
//        {
//            await RewardWriteAsync("e");
//            reward_state = 1;
//        }

//        else if(serial_reward_outputbuf == "rc")
//        {
//            reward_state = 0;
//        }
//        break;

//    case 3: //Extended, wait for lick
//        //If    if "lc" read -> record arm as rewarded, write "r" and go to state 5
//        //Else  if "to" read -> go to state 4
//        if(serial_reward_outputbuf == "lc")
//        {
//            trial_maze_dir_of_last_reward = trial_maze_dir;
//            await RewardWriteAsync("r");
//            reward_state = 5;
//        }

//        else if(serial_reward_outputbuf == "to")
//        {
//            reward_state = 4;
//        }
//        break;

//    case 4: //Lick Time out
//            //If    nothing. Record Time out, write "r" and go to state 5 
//            //Else  nothing
//        trial_maze_dir_of_last_reward = trial_maze_dir;
//        await RewardWriteAsync("r");
//        reward_state = 5;
//        break;

//    case 5: //Retracting
//        //If    if "rc" achieved, go to state 0
//        //Else  nothing
//        if(serial_reward_outputbuf == "rc")
//        {
//            reward_state = 0;
//        }
//        break;                       

//}














//if (valid_reward_loc == true)
//{
//    //send extend sig
//    await RewardWriteAsync("e");
//}

//if (valid_reward_loc == false)
//{
//    //send retract sig
//    await RewardWriteAsync("r");
//}

////Read buffers
//if (serial_reward_outputbuf == "ec") //if extend complete
//{

//}

//if (serial_reward_outputbuf == "rc") //if retract complete
//{

//}

//if (serial_reward_outputbuf == "l") //if reward given
//{

//}