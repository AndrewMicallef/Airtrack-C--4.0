
using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;

using System;
using System.Diagnostics;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using Windows.Storage.Streams;
using System.Text;

namespace RealTimeGraph
{
    partial class LineGraph
    {
        ///------------------------------------------ GLOBAL VARIABLES --------------------------------------------------------------

        /// UI Button Global Variables 
        bool IMU_stream_state = false;
        bool IMU_graph_en_state = false;
        bool folder_state = false;
        bool PIXY_plot_en_state = false;
        bool PIXY_graph_clear_state = false;
        bool serial_stream_state = false;

        /// IMU Global Variables                  
        IMetaWearBoard metawear;
        ISensorFusionBosch sensorfusion;
        ISettings settings;

        /// Stream Port + File Global Variables
        string file_name;
        Windows.Storage.StorageFolder folder;
        Windows.Storage.StorageFolder mainFolder;
        Windows.Storage.StorageFile file;
        IRandomAccessStream csv_stream;

        /// CSV Data Global Variables
        public int i = 0;
        public string buffer_string = "";
        public string string1 = "";
        public volatile IData imu_data;
        public bool data_save_en = false;
        StringBuilder builder = new StringBuilder();

        float data_q_H_buffer;
        string array3_buffer;
        string array4_buffer;
        string angle_actual_maze_buffer;
        string oxyplot_x_buffer;
        string oxyplot_y_buffer;

        /// Timer Global Variables
        double time_base_ms = 0;
        double time_base_sec = 0;
        double time_base_min = 0;
        double time_base_hour = 0;
        double base_elapsed_millisec = 0;
        double time_current_ms = 0;
        double time_current_sec = 0;
        double time_current_min = 0;
        double time_current_hour = 0;
        double current_elapsed_millisec = 0;

        /// Stream OXY Plot Global Variables
        double trial_time_ms = 0;
        bool time_base_set = false;
        float angle_maze = 0;
        float angle_actual_maze = 0;

        /// Arduino Uno Serial Port Global Variables
        DataReader UnoPixydataReader;
        SerialDevice serialPortUnoPixy;

        DataWriter DueRewarddataWriter;
        SerialDevice serialPortDueReward_Write;

        DataReader UnoRewarddataReader;
        SerialDevice serialPortUnoReward_Read;

        /// Arduino Uno PIXY Data Global Arrays Variables
        string array1 = "";
        string array2 = "";
        string array3 = "0";
        string array4 = "0";

        /// Arduino Uno PIXY Data Global Arrays Variables
        string serial_reward_buf1 = "";
        string serial_reward_buf2 = "";
        string serial_reward_outputbuf = "";

        /// Graph readout Global Variables
        int zerox = 0;
        int zeroy = 0;
        int arm_length = 130; //112
        int arm_width = 70; //35

        public float zeroangle = 0;
        public int x_coor = 0;
        public int y_coor = 0;
        public int x_disp = 0;
        public int y_disp = 0;
        public volatile bool x_neg_dir = false;
        public volatile bool y_neg_dir = false;
        public int radius = 0;
        public float angle = 0;
        public float angle2 = 0;

        public double x_post = 0;
        public double y_post = 0;

        public volatile bool x_y_plot_same = false;
        public double buffer_x = 0;
        public double buffer_y = 0;

        double processed_x = 0;
        double processed_y = 0;

        double oxyplot_x = 0;
        double oxyplot_y = 0;

        public double last_oxyplot_x = 0;
        public double last_oxyplot_y = 0;

        public bool oxyplot_within_boundaries = false;

        /// Automatic Reward Function Public Variable
        public bool auto_reward_en = false;
        public bool trial_new_arm = false;
        public int trial_maze_dir = 0;
        public int trial_maze_dir_of_last_reward = 0;
        public bool valid_reward_loc = false;
        public bool trial_reach_end_arm = false;
        public bool trial_reward_given = false;
        bool valid_reward_arm = false;
        int reward_state = 0;
        int min_x_disp = 105;
        int min_y_disp = -15;
        int max_y_disp = 15;


        /// Timers for update rates
        public Stopwatch trial_timer = new Stopwatch();
        public Stopwatch plotupdate_timer = new Stopwatch();        
        public TimeSpan period = TimeSpan.FromMilliseconds(100);

        // Create a timer        
        DispatcherTimer dispatcherTimer10Hz;
        DateTimeOffset startTime10Hz;
        DateTimeOffset lastTime10Hz;        
        int timesTicked10Hz = 1;

        DispatcherTimer dispatcherTimer100Hz;
        DateTimeOffset startTime100Hz;
        DateTimeOffset lastTime100Hz;
        int timesTicked100Hz = 1;


        // Trial start Variables
        public bool trial_active = false;
        public bool plot_mode_hist_en = false;
        public string buffer_trial_start = "";
        public string buffer_trial_stop = "";

        //Program Boolean Values
        public bool func_IMU_init_success = false;

        public bool func_IMU_euler_init_success = false;
        public bool func_IMU_euler_init_already_trig = false;

        public bool func_serial_init_success = false;

        public bool func_pixy_read_success = false;
        public bool func_pixy_read_already_trig = false;


        public bool func_folder_made_success = false;

        public bool func_plot_saved_success = false;

        public bool func_x_outline_drawn_success = false;

        public bool func_oxy_clear_success = false;

        public bool toggle_zero_set_success = false;

        public bool toggle_data_rec_set_success = false;
        public bool toggle_data_rec_set_already_trig = false;

        public bool toggle_plot_hist_set_success = false;

        public bool toggle_IMU_plot_en_success = false;
        public bool toggle_PIXY_plot_en_success = false;



    }
}
