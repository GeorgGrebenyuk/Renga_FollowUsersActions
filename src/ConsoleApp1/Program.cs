using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeometryGym.Ifc;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("end\n");
            Console.ReadKey();
        }
        static void ifc_test()
        {
            string ifc_path = @"D:\Work\Temp\House.ifc";
            DatabaseIfc file = new DatabaseIfc(ifc_path);

        }
    }
}
