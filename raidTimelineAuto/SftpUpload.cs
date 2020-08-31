using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace raidTimelineAuto
{
	class SftpUpload
	{
		public void UploadFile(string path, string folderPath)
		{
            FileInfo f = new FileInfo(path);
            string uploadfile = f.FullName;
            Console.WriteLine(f.Name);
            Console.WriteLine("uploadfile " + uploadfile);

			//Passing the sftp host without the "sftp://"
			var client = new SftpClient("", 22, "", "");
            client.Connect();
            if (client.IsConnected)
            {
                var fileStream = new FileStream(uploadfile, FileMode.Open);
                if (fileStream != null)
                {
                    //If you have a folder located at sftp://ftp.example.com/share
                    //then you can add this like:
                    client.UploadFile(fileStream, "" + folderPath + f.Name, null);
                    client.Disconnect();
                    client.Dispose();
                }
            }
        }
	}
}
