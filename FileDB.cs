using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerData;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;


public class FileData
{
    string fileOwner;
    string ipAddress;
    int portNumber;
    List<string, string, int> fileInfo;

	public FileData(string fo, string ia, int pn)
	{
        new fileInfo<fo, ia, pn>();
	}
}
