 using UnityEngine;
 using System;
 using System.IO;
 public static class AppHelper
 {
    public static string webplayerQuitURL = "http://google.com";
    public static DateTime exporting_end_time = DateTime.MinValue;
    public static TimeSpan exporting_remaining_time;
    public static DateTime training_end_time=DateTime.MinValue;
     public static TimeSpan training_remaining_time;
     public static float training_time;
    public static float threshold_volume=75f;
    public static string early_stopping;
    public static string file_path;

    public static string fbx_file_path;

    public static string instructions_file_path;

    public static string log_base_path;

    public static string uuid;

    public static bool running_inference = false;

    public static bool running_training = false;

    public static string homeDir;



    

     public static void Quit()
     {
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.Exit(0);
        }
         else if (Application.platform == RuntimePlatform.WebGLPlayer)
         {
            Application.OpenURL(webplayerQuitURL);
         }
         else
         {
            Application.Quit();
         }
     }

     public static bool StartTimer(string flag)
     {
        // for stopping environment exporting fbx
        if (flag == "exporting")
        {
            // shut down environment 3 minutes from now
            if (exporting_end_time==DateTime.MinValue)
            {
                exporting_end_time = DateTime.Now.AddSeconds(10);
            }
            exporting_remaining_time = exporting_end_time - DateTime.Now;
            if  (exporting_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        // for training with a time limit
        else
        {   // Set the end time to 3 minutes from now
            if (training_end_time==DateTime.MinValue)
            {
                training_end_time = DateTime.Now.AddMinutes(training_time);
            }
            training_remaining_time = training_end_time - DateTime.Now;
            if  (training_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
     }

    public static void GetCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();
        homeDir = Environment.GetEnvironmentVariable("HOME"); // AWS: /home/ubuntu/
        //Debug.Log("Command line arguments passed: " + String.Join(" ", args));
        for (int i = 0; i < args.Length; i++)
        {
            //Debug.Log($"CXX args: {args[i]}");
            if (args[i] == "inference")
            {
                running_inference = true;
            }
            if (args[i] == "training")
            {
                running_training = true;
            }
            if (args[i].StartsWith("volume"))
            {
                threshold_volume = float.Parse(args[i+1]);
                early_stopping = "volume";
            }
            if (args[i].StartsWith("time"))
            {
                training_time = float.Parse(args[i+1]);
                early_stopping = "time";
            }
            if (args[i] == "path")
            {
                file_path = args[i+1];
                uuid = Path.GetFileNameWithoutExtension(file_path);
            }         
        }
        fbx_file_path = Path.Combine($"{homeDir}", "React3D/public/", "fbx", $"{uuid}.fbx");
        instructions_file_path = Path.Combine($"{homeDir}", "React3D/public/", "instructions", $"{uuid}.txt");
        log_base_path = Path.Combine($"{homeDir}", "React3D/public/log/");
    }

     public static void EndTraining()
    {
        if (StartTimer("exporting"))
        {
            Quit();
        }
    }

     

     public static void LogStatus(string type)
     {
        string line = "";
        string path = "";
        if (type=="fbx")
        {
            line = $"FBX {uuid} exported on {DateTime.Now.ToString("HH:mm:ss tt")}";
            path = Path.Combine(log_base_path, "fbx_log.txt");;
        }
        else if (type == "instructions")
        {
            line = $"INSTRUCTION {uuid} exported on {DateTime.Now.ToString("HH:mm:ss tt")}";
            path =  Path.Combine(log_base_path, "instruction_log.txt");
        }
        if (!File.Exists(path))
        {
            // Write the string to a file.
            StreamWriter file = new StreamWriter(path);
            file.WriteLine(line);
            file.Close();
        }
        else 
        {
            using (StreamWriter w = File.AppendText(path))
            {
                w.WriteLine(line);

            }
        }
     }

 }