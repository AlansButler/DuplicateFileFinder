using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DuplicateFileFinder
{
    public partial class Form1 : Form
    {
        public enum CompareResult { different, sameSize, sameHash, sameByteForByte };

        List<string> bypassList = new List<string> { @"Camping\Oregon", @"Moms1\For50thAnni", "CoolPics"};
        
        Thread th;
        bool wannaClose = false;
        //bool bkThreadClosed = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            wannaClose = false;
            //bkThreadClosed = true;


            (sender as Button).Enabled = false;


            //ThreadStart ts = new Thread(CallFindDups);
            th = new Thread(CallFindDups);
            th.Name = "FindDups Thread";
            th.Priority = ThreadPriority.BelowNormal;
            th.Start(); // th.Name);
            //bkThreadClosed = false;

            Thread.Sleep(1000);  //Give background thread plenty of time to enter Running state.

            // Loop until worker thread activates.
            while (!th.IsAlive)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }

            while ((!wannaClose) && (th.ThreadState != System.Threading.ThreadState.Stopped))
            {
                Thread.Sleep(100);
                Application.DoEvents();
            }

            // Use the Join method to block the current thread 
            // until the object's thread terminates.
            //th.Join();

            if (wannaClose)
            {
                while (th.IsAlive)
                {
                    Thread.Sleep(1000);
                    Application.DoEvents();
                    th.Join();
                }
            }


            //Now move files from original list to delete folder
            if (tbOriginal.Lines.Count() > 0)
            {
                string fnDirOld = "XXX";
                List<string> filesToDelete = new List<string>();
                string driveLetter = Path.GetDirectoryName(tbOriginal.Lines[0]).Substring(0, 1);
                filesToDelete.Add(driveLetter + ':');   //Make the current drive the correct one.
                foreach (string feyle in tbOriginal.Lines)
                {
                    string fnDir = Path.GetDirectoryName(feyle);
                    string fnOnly = Path.GetFileNameWithoutExtension(feyle);
                    if ( (fnOnly.Contains(" ")) || (fnOnly.Contains("&")) )
                        fnOnly = String.Concat(@"""", fnOnly, @"""");
                    if ( (fnDir.Contains(" ")) || (fnDir.Contains("&")) )
                        fnDir = String.Concat(@"""", fnDir, @"""");
                    string fnExt = Path.GetExtension(feyle);
                    if (fnDirOld != fnDir)
                        filesToDelete.Add("CD " + fnDir);
                    filesToDelete.Add("erase " + fnOnly + fnExt);
                    fnDirOld = fnDir;
                    //File.Move(feyle, @"D:\wwwroot\images\delete\" + fnOnly);
                }
                string batchFileName = "FilesToDel" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture) + ".bat";
                string batchFileOutputPath = ConfigurationSettings.AppSettings["batchFileOutputPath"];
                File.WriteAllLines(batchFileOutputPath + batchFileName, filesToDelete);
            }

            Application.DoEvents();

            //bkThreadClosed = true;

            System.Media.SystemSounds.Asterisk.Play();
            //FindDups(DirFirst, DirSecond);
            (sender as Button).Enabled = true;
        }

        void CallFindDups()
        {
            //System.Configuration.AppSettingsReader appSettingsReader = new AppSettingsReader();
            //string dirFirst = appSettingsReader.GetValue("dirFirst", dirFirst);

            string dirFirst = ConfigurationSettings.AppSettings["dirFirst"];
            string dirSecond = ConfigurationSettings.AppSettings["dirSecond"];


            //string DirFirst = ConfigurationManager.AppSettings["dirFirst"];
            //st/ring DirSecond = System.Configuration.ConfigurationManager.AppSettings["dirSecond"];
            FindDups(dirFirst, dirSecond);


        }

        private bool Bypass(string pathOnlyFile1, string pathOnlyFile2)
        {
            bool result = false;
            var e = bypassList.GetEnumerator();
            while ((!result) && (e.MoveNext()))
            {
                //result = pathOnly.Contains(e.Current);  //contains is case sensitive.  So you have to upper both sides and still do a case sensitive compare.
                result =
                    (pathOnlyFile1.IndexOf(e.Current, StringComparison.CurrentCultureIgnoreCase) >= 0)
                                            ||
                    (pathOnlyFile2.IndexOf(e.Current, StringComparison.CurrentCultureIgnoreCase) >= 0);


            }

            return result;
        }

        private void FindDups(string DirFirst, string DirSecond)
        {

            List<string> filesListFirst;
            List<string> filesListSecond;

            TraverseTree(DirFirst, out filesListFirst);
            TraverseTree(DirSecond, out filesListSecond);

            List<string> OrigList = new List<string>();
            List<string> DupList = new List<string>();
            MethodInvoker m;

            m = new MethodInvoker(() =>
            {
                progressBar1.Maximum = filesListFirst.Count();
                progressBar1.Value = 1;
            }
            );
            this.Invoke(m);

            foreach (string file1 in filesListFirst)
            {
                if (wannaClose)
                {
                    return;
                }

                //this.Text = file1;
                m = new MethodInvoker(() => this.Text = file1);
                this.Invoke(m);
                //if app is not active ActiveForm will be null
                /*
                if (Form1.ActiveForm != null)
                {
                //    //Form1.ActiveForm.Text = file1; // + ":" + fileName2;
                    this.Text = file1;
                }
                */

                /////////////////
                /*
                string pathOnly = Path.GetDirectoryName(file1);
                if  (pathOnly.EndsWith("_vti_cnf"))
                {
                    
                    DirectoryInfo di = new DirectoryInfo(pathOnly);
                    if (di.Exists)
                    {
                        if (!OrigList.Contains(pathOnly))
                        {
                            //di.Delete(true);
                            OrigList.Add(pathOnly);
                        }
                    }
                    
                }
                */
                //////////////
                m = new MethodInvoker(() => progressBar1.Increment(1));
                this.Invoke(m);


                foreach (string file2 in filesListSecond)
                {
                    //skip id Path1 and Path2 are the same
                    //bool cond1 = ((Path.GetDirectoryName(file1) != Path.GetDirectoryName(file2)) && (file1 == file2));
                    //bool cond2 = ((Path.GetDirectoryName(file1) == Path.GetDirectoryName(file2)) && (file1 != file2));
                    string pathOnlyFile1 = Path.GetDirectoryName(file1);
                    string pathOnlyFile2 = Path.GetDirectoryName(file2);
                    if ( (!file1.Equals(file2, StringComparison.CurrentCultureIgnoreCase)) && (!Bypass(pathOnlyFile1, pathOnlyFile2)) ) //file1 and 2 include the path. So avoid checking file in same folder aginst itself
                    //if ( (file1 != file2) && (!Bypass(pathOnlyFile1, pathOnlyFile2)) ) //file1 and 2 include the path. So avoid checking file in same folder aginst itself
                    {
                        // public enum CompareResult { different, sameSize, sameHash, sameByteForByte };
                        CompareResult compareResult = CompareFiles(file1, file2);
                        if (compareResult > CompareResult.different)
                        {
                            bool alreadyLoggedBackwards = (OrigList.Contains(file2)) && (DupList.Contains(file1));
                            if (!alreadyLoggedBackwards)  //to avoid adding same pair in reverse.
                            {
                                OrigList.Add(file1 + " (" + compareResult.ToString() + ")");
                                DupList.Add(file2);
                            }
                        }
                        //if (compareResult == CompareResult.sameByteForByte)
                        //{
                        //    bool alreadyLoggedBackwards = (OrigList.Contains(file2)) && (DupList.Contains(file1));
                        //    if (!alreadyLoggedBackwards)  //to avoid adding same pair in reverse.
                        //    {
                        //        OrigList.Add(file1);
                        //        DupList.Add(file2);
                        //    }
                        //}
                        //else
                        //{
                        //    if ( (compareResult >= CompareResult.sameSize) && (compareResult <= CompareResult.sameHash) )
                        //    {
                        //        bool alreadyLoggedBackwards = (OrigList.Contains(file2)) && (DupList.Contains(file1));
                        //        if (!alreadyLoggedBackwards)  //to avoid adding same pair in reverse.
                        //        {
                        //            OrigList.Add(file1 + "***SameSizeOnly***");
                        //            DupList.Add(file2 + "***SameSizeOnly***");
                        //        }
                        //    }
                        //}
                    }
                }

                //Thread.Sleep(10);
                //if ((DateTime.Now.Second % 5) == 0)
                     //Application.DoEvents();
            }

            
            m = new MethodInvoker(() => tbOriginal.Lines = OrigList.ToArray());
            this.Invoke(m);
            m = new MethodInvoker(() => tbDuplicate.Lines = DupList.ToArray());
            this.Invoke(m);


            tbOriginal.Lines = OrigList.ToArray();
            tbDuplicate.Lines = DupList.ToArray();
            

        }


        public CompareResult CompareFiles(string fileName1, string fileName2)
        {
            FileInfo info1 = new FileInfo(fileName1);
            FileInfo info2 = new FileInfo(fileName2);
            if (info1.Length != info2.Length) 
            {
                return CompareResult.different;
            }
            if ( (GetHashCode(fileName1, new SHA512CryptoServiceProvider())) != (GetHashCode(fileName2, new SHA512CryptoServiceProvider())) )
            {
                return CompareResult.sameSize;
            }
            FileStream fs1 = info1.OpenRead();
            FileStream fs2 = info2.OpenRead();
            BufferedStream bs1 = new BufferedStream(fs1);
            BufferedStream bs2 = new BufferedStream(fs2);

            try
            {
                for (long i = 0; i < info1.Length; i++)
                {
                    if (bs1.ReadByte() != bs2.ReadByte())
                    {
                        return CompareResult.sameHash;
                    }
                }
                    
            }
            finally
            {
                bs1.Close();
                bs2.Close();
            }

            return CompareResult.sameByteForByte;
        }

        internal string GetHashCode(string filePath, HashAlgorithm cryptoService)
        {
            // create or use the instance of the crypto service provider
            // this can be either MD5, SHA1, SHA256, SHA384 or SHA512
            using (cryptoService)
            {
                using (var fileStream = new FileStream(filePath,
                                                       FileMode.Open,
                                                       FileAccess.Read,
                                                       FileShare.ReadWrite))
                {
                    var hash = cryptoService.ComputeHash(fileStream);
                    var hashString = Convert.ToBase64String(hash);
                    return hashString.TrimEnd('=');
                }
            }
        }


        /*
          WriteLine("MD5 Hash Code   : {0}", GetHashCode(FilePath, new MD5CryptoServiceProvider()));
          WriteLine("SHA1 Hash Code  : {0}", GetHashCode(FilePath, new SHA1CryptoServiceProvider()));
          WriteLine("SHA256 Hash Code: {0}", GetHashCode(FilePath, new SHA256CryptoServiceProvider()));
          WriteLine("SHA384 Hash Code: {0}", GetHashCode(FilePath, new SHA384CryptoServiceProvider()));
          WriteLine("SHA512 Hash Code: {0}", GetHashCode(FilePath, new SHA512CryptoServiceProvider
        */

        public static void TraverseTree(string root, out List<string> fyles)
        {
            // Data structure to hold names of subfolders to be examined for files.
            Stack<string> dirs = new Stack<string>(20);

            fyles = new List<string>();

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException(root + " not found");
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        fyles.Add(file);
                        // Perform whatever action is required in your scenario.
                        //System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        //Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }

        private void tbOriginal_DoubleClick(object sender, EventArgs e)
        {

            //int lineNum = GetFirstCharIndexFromLine((sender as TextBox).SelectionStart) + 1;

            //(sender as TextBox).SelectedText;
            //            (sender as TextBox)

            //          Process.Start((sender as TextBox).SelectedText);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            wannaClose = true;

            //th.Abort();
            //while (th.ThreadState != System.Threading.ThreadState.Stopped)  //Wait for bkgrnd thread to close.  Otherwise it'll try to write to a disposed form.
            //{
                //Application.DoEvents();
                //Thread.Sleep(500);
            //}

        }
    }
}
