using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyIoC
{
    public class Customer
    {
    }
    [ImportConstructor]
	public class CustomerBLL : Customer
    {
        public ICustomerDAL CustomerDAL { get; set; }
        public Logger logger { get; set; }
        public CustomerBLL(ICustomerDAL dal, Logger logger)
		{
            this.CustomerDAL = dal;
            this.logger = logger;
        }
	}

	public class CustomerBLL2 : Customer
    {
        [Import]
        public ICustomerDAL CustomerDAL; //{ get; set; }
		[Import]
		public Logger logger { get; set; }
	}
}
