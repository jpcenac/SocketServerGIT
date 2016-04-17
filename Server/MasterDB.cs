using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class FileInfo
    {
    string fileName;
    string ipAdress;
    int portNumber;
    List<Tuple<string, int>> masterList;

        public FileInfo(string ip, int pn, object dataClient)
        {
            Tuple<string, int> fileInfo = new Tuple<string,int>(ip, pn);
        }

        //public List<Tuple<string, int>> addToList()
        //{

        //}
    }
}
