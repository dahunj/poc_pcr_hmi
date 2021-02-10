using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABI_POC_PCR
{
    public class SharedMemory
    {
        public string userID { get; set; }
        public string userPW { get; set; }
        public string userName { get; set; }

        public string MTBC_Result { get; set; }
        public string NTM_Result { get; set; }
        public string RIF_Result { get; set; }
        public string IC_Result { get; set; }

        public int criticalThreshold { get; set; }
        
        private static SharedMemory _instance = null;

        public static SharedMemory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SharedMemory();
            }
            return _instance;
        }
    }
}
