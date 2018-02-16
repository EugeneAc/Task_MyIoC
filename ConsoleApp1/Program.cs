using MyIoC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {

        static void Main(string[] args)
        {
            var container = new MyIoC.Container();
            container.AddAssembly(Assembly.GetExecutingAssembly());

            container.AddType(typeof(CustomerBLL));
            //container.AddType(typeof(Logger));
            container.AddType(typeof(CustomerDAL), typeof(ICustomerDAL));

            var customerBLL = (CustomerBLL)container.CreateInstance(typeof(CustomerBLL));
            Console.WriteLine(customerBLL);
            var customerBLL2 = container.CreateInstance<CustomerBLL2>();
            Console.WriteLine(customerBLL2);

            Console.ReadLine();
        }
    }
}
