using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var unit = new XFileTest("../../models/x/test.x");
            unit.SetUp();
            unit.TestCameras();
        }
    }
}
