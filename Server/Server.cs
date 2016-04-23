using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace Server
{
    class Server
    {

        static Socket listenerSocket;
        //filename...filePath, IPAdress, portnumber
        static Dictionary<string, List<Tuple<string, string, int>>> masterDB;

        //start server
        static void Main(string[] args)
        {
            masterDB = new Dictionary<string, List<Tuple<string, string, int>>>();
            Console.Write("Starting server on: ");

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           
            //IPAddress = System.Net.IPAddress.Parse(Server);
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 30000);
            listenerSocket.Bind(serverIP);
            Console.WriteLine(serverIP.ToString());

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();
        }

        //listener: listens for clients
        static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                ClientData newClient = new ClientData(listenerSocket.Accept());            
            }
        }
       

        public static void SocketSendString(Socket inSock, string input)
        {
            try
            {
                //Console.WriteLine("Sending: " + input);            
                input = input + "<EOF>";
                inSock.Send(Encoding.ASCII.GetBytes(input));
            }
            catch(Exception e)
            {
                
                Console.WriteLine(e);
            }
        }

        //with flag
        public static string Data_Receive2(object cSocket)
        {
            string clientData = null;
            Socket clientSocket = (Socket)cSocket;
            byte[] Buffer;
            
            try
            {
                while (true)
                {
                    Buffer = new Byte[1024];
                    int received = clientSocket.Receive(Buffer);
                    // decode data sent
                    clientData += Encoding.ASCII.GetString(Buffer, 0, received);
                    //Console.WriteLine("Attempting Receive");
                    if (clientData.IndexOf("<EOF>") > -1)
                    {
                        //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        //clientData.Length, clientData);
                        break;
                    }

                }
                return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
            }
            catch
            {
               // Console.WriteLine(e);


                return "UnexpectedDisc";
            }
            //return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("IP was not found");
        }

        class ClientData
        {
            public Socket clientSocket;
            public Thread clientThread; //
            public string filePath;
            private string thisClientIP;
            private int thisClientPort;

            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                filePath = string.Empty;
                clientThread = new Thread(ServerFunction);
                clientThread.Start();
            }

            //public void OnReceive(IAsyncResult result)
            //{
            //    try
            //    {
            //        var bytesReceived = this.clientSocket.EndReceive(result);

            //        if (bytesReceived <= 0)
            //        {
            //            // normal disconnect
            //            return;
            //        }

            //        // ...

            //        //this.Socket.BeginReceive...;
            //    }
            //    catch // SocketException
            //    {
            //        // abnormal disconnect
            //    }
            //}

            public void ServerFunction()
            {
                try
                {
                    string servControl = string.Empty;
                    string parseClientInfo = Data_Receive2(clientSocket);
                    string[] clientIPPort = parseClientInfo.Split(';');
                    thisClientIP = clientIPPort[0];
                    thisClientPort = int.Parse(clientIPPort[1]);
                    Console.WriteLine("Client IP  : " + thisClientIP);
                    Console.WriteLine("Client Port: " + thisClientPort.ToString());
                    //Console.WriteLine("Client Thread started");
                    //SocketSendString(clientSocket, "You are client: " + filePath);
                    //AcceptFileInfo();
                    Console.WriteLine("Client Has Connected");

                    while (true)
                    {
                        servControl = Data_Receive2(clientSocket);
                        switch (servControl)
                        {
                            case "RequestFileList":
                                SendFileInfo();
                                break;

                            case "UpdateFileServer":
                                AcceptFileInfo();
                                break;

                            case "PrintDataBase":
                                CheckDatabase();
                                break;

                            case "DownloadFile":
                                try
                                {
                                    Tuple<string, string, int> clientInfo;
                                    Console.WriteLine("Server set to Send FileName for Download");
                                    SocketSendString(clientSocket, "SERVER//Type FileName with extension");
                                    string receiveFile = Data_Receive2(clientSocket);
                                    if (masterDB.Keys.Contains(receiveFile))
                                    {
                                        SocketSendString(clientSocket, "Correct");
                                        clientInfo = CheckFileInfo(receiveFile);
                                        string HostInfo = stringifyTuple(clientInfo);
                                        SocketSendString(clientSocket, HostInfo);

                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Client Entered Incorrect Filename");
                                        SocketSendString(clientSocket, "Incorrect");
                                    }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    break;
                                }
                                break;
                            case "Disconnecting":

                                RemoveFileInfo(thisClientIP, thisClientPort.ToString());
                                SocketSendString(clientSocket, "RemoveFileInfoSuccess");
                                clientSocket.Close();
                                Console.WriteLine("Client Has Disconnected");
                                Thread.CurrentThread.Abort();
                                break;

                            case "UnexpectedDisc":
                                Console.WriteLine("Socket was most likely Forcibly Removed, Data Receive is being Terminated");
                                Console.WriteLine("Attempting to Remove Client Info");
                                Console.WriteLine("C IP  :" + thisClientIP);
                                Console.WriteLine("C Port: " + thisClientPort.ToString());
                                RemoveFileInfo(thisClientIP, thisClientPort.ToString());
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                                Thread.CurrentThread.Abort();
                                break;
                            default:
                                Console.WriteLine("Default Case");
                                //SocketSendString(clientSocket, "In Default Case Currently");
                                break;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Aborting Thread, Connection to Client Lost");
                    clientSocket.Close();
                    Thread.CurrentThread.Abort();
                    return;
                }
            }

            public void AcceptFileInfo()
            {

                string fileCountString = Data_Receive2(clientSocket);
               
                int fileCount = int.Parse(fileCountString);
                SocketSendString(clientSocket, "FileCountReceived");
                
                for(int i = 0; i<fileCount; i++)
                {
                    string fileName = Data_Receive2(clientSocket);
                    //Console.WriteLine(fileName);
                    SocketSendString(clientSocket, "FNReceived");
                    string filePath = Data_Receive2(clientSocket);
                    //Console.WriteLine(filePath);
                    SocketSendString(clientSocket, "FPReceived");
                    string commenceCheck = Data_Receive2(clientSocket);
                    List<Tuple<string, string, int>> fileInfoList = new List<Tuple<string, string, int>>();
                    Tuple<string, string, int> fileInfo = new Tuple<string, string, int>(filePath, thisClientIP, thisClientPort);
                    //Console.WriteLine(commenceCheck);
                    if(!masterDB.ContainsKey(fileName))
                    {
                        fileInfoList.Clear();
                        Console.WriteLine("Adding " + fileName + " to Master Database");

                        Console.WriteLine(fileName + ": " + fileInfo.Item1 + " " + fileInfo.Item2+ " " + fileInfo.Item3.ToString());
                        fileInfoList.Add(fileInfo);
                        masterDB.Add(fileName, fileInfoList);
                    }
                    else
                    {
                        //REDO THIS JANK ASS CODE FUCK YOU
                        //Console.WriteLine(fileName);
                        List<Tuple<string, string, int>> dupTupleList = masterDB[fileName];
                       
                        foreach(Tuple<string, string, int> compTuple in masterDB[fileName].ToList())
                        {
                           
                            if (fileInfo.Item1 == compTuple.Item1 && fileInfo.Item2 == compTuple.Item2 && fileInfo.Item3 == compTuple.Item3)
                            {
                                Console.WriteLine("Duplicate File Found");
                                masterDB[fileName] = masterDB[fileName].Distinct().ToList();
                                
                            }
                            else if (fileInfo.Item1 != compTuple.Item1 & fileInfo.Item2 != compTuple.Item2 || fileInfo.Item3 != compTuple.Item3)
                            {
                                if (fileInfo.Item1 != compTuple.Item1)
                                {
                                    Console.WriteLine(fileInfo.Item2 + " /// " + compTuple.Item2);
                                    Console.WriteLine(fileInfo.Item3.ToString() + " /// " + compTuple.Item3.ToString());

                                    Console.WriteLine("Same Filename, Different Path, Adding");
                                    masterDB[fileName].Add(fileInfo);
                                    Console.WriteLine("FILEINFO HOSTS: masterDB[fileName].Count.ToString()");
                                }
                                else
                                {
                                    Console.WriteLine(fileInfo.Item2 + " /// " + compTuple.Item2);
                                    Console.WriteLine(fileInfo.Item3.ToString() + " /// " + compTuple.Item3.ToString());
                                    
                                    Console.WriteLine("Adding new fileInfo to File");
                                    masterDB[fileName].Add(fileInfo);
                                    Console.WriteLine("FILEINFO HOSTS: masterDB[fileName].Count.ToString()");
                                }
                                
                            }
                            else
                            {
                                Console.WriteLine("Doing Nothing here");
                            }
                        }
                    }
                }
            }

            public void CheckDatabase()
            {
                foreach(string fn in masterDB.Keys)
                {
                    Console.WriteLine("DB has " + masterDB[fn].Count.ToString() + " on Record");
                   Console.WriteLine(fn + " :::::::::::: ");
                    foreach(Tuple<string, string, int> tupPrint in masterDB[fn].ToList())
                    {                           
                        Console.WriteLine(tupPrint.Item1 + "\n:::" + tupPrint.Item2 + "\n:::" + tupPrint.Item3);
                       
                        Console.WriteLine();
                    }
                }
            }

            public void SendFileInfo()
            {
                Console.WriteLine("Request for File List Received");
                SocketSendString(clientSocket, "SERVER: SendingFileNames");
                string readyConfirm = Data_Receive2(clientSocket);
                string FileList = string.Empty;
                foreach(string fn in masterDB.Keys)
                {
                    //Console.WriteLine("Sending: " + fn);
                    FileList = FileList + string.Concat(fn, ":");
                    
                }
                Console.WriteLine(FileList);
                SocketSendString(clientSocket, FileList);
            }

            public Tuple<string, string, int> CheckFileInfo(string fileChoice)
            {
                
                Console.WriteLine("FileRequest: " + fileChoice);
            
                //Console.WriteLine(masterDB[fileChoice] == null?"file does not exist" : "file exists in database");
                foreach(Tuple<string, string, int> clientTuple in masterDB[fileChoice])
                {
                    Console.WriteLine("Returning with Valid Client Info");
                    return clientTuple;
                }
                Console.WriteLine("Returning Null");
                return null;
            }

            public string stringifyTuple(Tuple<string, string, int> cInfo)
            {
                string filePath = cInfo.Item1;
                string ipAddress = cInfo.Item2;
                string portNum = cInfo.Item3.ToString();
                string ipAndPort = String.Concat(ipAddress, ";", portNum);

                string allClietInfo = String.Concat(filePath, ";", ipAndPort);

                return allClietInfo;
            }

            public void RemoveFileInfo(string clientIP, string clientPort)
            {
                int clientPortInt = int.Parse(clientPort);

                foreach(string fileName in masterDB.Keys)
                {
                    foreach(Tuple<string, string, int> targetTuple in masterDB[fileName])
                    {
                        if(targetTuple.Item2 == clientIP && targetTuple.Item3 == clientPortInt)
                        {

                            if(masterDB[fileName].Count > 1)
                            {
                                Console.WriteLine("DB has " + masterDB[fileName].Count.ToString() + " on Record");
                                Console.WriteLine("Removing IP  : " + clientIP);
                                Console.WriteLine("Removing Port: " + clientPort);
                                Console.WriteLine("FileName: " + fileName + " Remains");
                                masterDB[fileName].Remove(targetTuple);
                                RemoveFileInfo(clientIP, clientPort);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Removing IP  : " + clientIP);
                                Console.WriteLine("Removing Port: " + clientPort);
                                Console.WriteLine("Last Host for File, File Removed");

                                masterDB.Remove(fileName);
                                RemoveFileInfo(clientIP, clientPort);
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                        
                    }

                    break;

                }
            }
            
        }
    }
}
