using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace deer_wanna_bot.Data
{
    public class Screen : BaseData
    {           
        public Screen(string name)
        {
            this.ScreenName = name;
        }

        public string ScreenName { get; private set; }

        bool Check(Mat screenMat)
        {            
            return false;
        }
    }
}
