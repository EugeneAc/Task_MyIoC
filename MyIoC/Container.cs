using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyIoC
{
    /// <summary>
    /// Общая часть
    ////Наша задача: разработать IoC-контейнер (следуя принципу «Каждый программист должен разработать свой IoC/DI контейнер» © ).
    ////В качестве примера мы возьмем Managed Extensibility Framework (MEF), в котором основная настройка контейнера происходит за счет расстановки атрибутов. 
    ////Но (!) весь код, включая объявление атрибутов у нас будет свой.
    ////Задание 1.
    ////Используя механизмы Reflection, создайте простейший IoC-контейнер, который позволяет следующее:
    ////•	Разметить классы, требующие внедрения зависимостей одним из следующих способов
    ////o	Через конструктор (тогда класс размечается атрибутом [ImportConstructor])
    ////[ImportConstructor]
    ////public class CustomerBLL
    ////{
    ////    public CustomerBLL(ICustomerDAL dal, Logger logger)
    ////    { }
    ////}

    ////o	Через публичные свойства (тогда каждое свойство, требующее инициализации,  размечается атрибутом [Import])
    ////public class CustomerBLL
    ////{
    ////    [Import]
    ////    public ICustomerDAL CustomerDAL { get; set; }
    ////    [Import]
    ////    public Logger logger { get; set; }
    ////}

    ////При этом, конкретный класс, понятное дело, размечается только одним способом!
    ////•	Разметить зависимые классы
    ////o	Когда класс используется непосредственно
    ////[Export]
    ////public class Logger
    ////{ }

    ////o	Когда в классах, требующих реализации зависимости используется интерфейс или базовый класс
    ////[Export(typeof(ICustomerDAL))]
    ////public class CustomerDAL : ICustomerDAL
    ////{ }

    ////•	Явно указать классы, которые зависят от других или требуют внедрения зависимостей
    ////var container = new Container();
    ////container.AddType(typeof(CustomerBLL));
    ////container.AddType(typeof(Logger));
    ////container.AddType(typeof(CustomerDAL), typeof(ICustomerDAL));

    ////•	Добавить в контейнер все размеченные атрибутами [ImportConstructor], [Import] и [Export], указав сборку
    ////var container = new Container();
    ////container.AddAssembly(Assembly.GetExecutingAssembly());

    ////•	Получить экземпляр ранее зарегистрированного класса со всеми зависимостями 
    ////var customerBLL = (CustomerBLL)container.CreateInstance(
    ////				typeof(CustomerBLL));
    ////var customerBLL = container.CreateInstance<CustomerBLL>();
    /// </summary>
    public class Container
	{
        private class ContainerItem
        {
            public Type Type { get; set; }
            public Type Contract { get; set; }
        }
        private List<ContainerItem> _exportTypes = new List<ContainerItem>(); //коллекция экспортных типов
        private List<ContainerItem> _importConstructorTypes = new List<ContainerItem>(); //коллекция типов с [ImportConstructor]
        private List<ContainerItem> _importTypes = new List<ContainerItem>(); // коллекция импортных типов

        /// <summary>
        /// Исследует сбоку на атрибуты [ImportConstructor], [Import] и [Export] и растасовывает типы по коллекциям
        /// </summary>
        /// <param name="assembly"></param>
        public void AddAssembly(Assembly assembly)
		{
            FillCollection(_exportTypes, assembly.GetTypes(), typeof(ExportAttribute));

            FillCollection(_importConstructorTypes, assembly.GetTypes(), typeof(ImportConstructorAttribute));

            var props = assembly.GetTypes().SelectMany(t => t.GetMembers()).Where(a => Attribute.IsDefined(a, typeof(ImportAttribute))).Where(x => x.MemberType == MemberTypes.Property).Select(m => (PropertyInfo)m).Select(p => p.PropertyType).ToArray(); //Свойства типов с ImportAttribute
            var fields = assembly.GetTypes().SelectMany(t => t.GetMembers()).Where(a => Attribute.IsDefined(a, typeof(ImportAttribute))).Where(x => x.MemberType == MemberTypes.Field).Select(m => (FieldInfo)m).Select(p => p.FieldType).ToArray(); //Поля типов с ImportAttribute

            FillCollection(_importTypes, props, typeof(Attribute));
            FillCollection(_importTypes, fields, typeof(Attribute));
        }
        private void FillCollection<T> (T fillingCollection,Type[] typesToCheck, Type attributeType) where T: List<ContainerItem>
        {
            var types = from t in typesToCheck
                        where (Attribute.IsDefined(t, attributeType))
                              select t;
            foreach (var t in types)
            {
                fillingCollection.Add(new ContainerItem() { Type = t, Contract = t });
            }
        }

        /// <summary>
        /// Добавляет тип в необходимую коллекцию
        /// </summary>
        public void AddType(Type type, Type baseType)
		{
            if (Attribute.IsDefined(type, typeof(ImportAttribute)))
            {
                _importTypes.Add(new ContainerItem() { Type = type, Contract = baseType });
            }

            if (Attribute.IsDefined(type, typeof(ExportAttribute)))
            {
                _exportTypes.Add(new ContainerItem() { Type = type, Contract = baseType });
            }

            if (Attribute.IsDefined(type, typeof(ImportConstructorAttribute)))
            {
                _importConstructorTypes.Add(new ContainerItem() { Type = type, Contract = baseType });
            }
        }
        public void AddType(Type type)
        {
            this.AddType(type, type);
        }
        /// <summary>
        /// При создании экземпляра методом CreateInstance, в зависимости от того как размечен тип атрибута метода
        /// вызывается либо первый конструктор класса, либо инициализируются поля/свойства
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object CreateInstance(Type type)
        {
            if (Attribute.IsDefined(type, typeof(ImportConstructorAttribute)))
            {
                var constructorInfos = type.GetConstructors().First().GetParameters().ToArray();
                object[] constructorAttributes = new object[constructorInfos.Length];
                for (int i = 0; i < constructorAttributes.Length; i++)
                {
                    constructorAttributes[i] = ResolveImportToExport(constructorInfos[i].ParameterType);
                }
                return Activator.CreateInstance(type, constructorAttributes);
            }
        
            var importProps = from t in type.GetMembers()
                           where (t.MemberType == MemberTypes.Property)
                           where (Attribute.IsDefined(t, typeof(ImportAttribute)))
                           select (PropertyInfo)t;

            var importFields = from t in type.GetMembers()
                              where (t.MemberType == MemberTypes.Field)
                              where (Attribute.IsDefined(t, typeof(ImportAttribute)))
                              select (FieldInfo)t;
            if ((importProps != null) || (importFields != null))
            {
                var T = Activator.CreateInstance(type);
                if (importProps != null)
                {
                    foreach (var impT in importProps)
                    {
                        impT.SetValue(T, ResolveImportToExport(impT.PropertyType));
                    }
                }
                if (importFields != null)
                {
                    foreach (var impF in importFields)
                    {
                         impF.SetValue(T, ResolveImportToExport(impF.FieldType));
                    }
                }
            return T;
            }
            return null;
		}

        /// <summary>
        /// Здесь логика совмещения типов с ImportAttribute к типам с ExportAttribute
        /// Если importType совпадает с контракторм экспорта, то создается экземпляр
        /// </summary>
        /// <param name="importType"></param>
        /// <returns></returns>
        private object ResolveImportToExport(Type importType)
        {
            foreach (var expT in _exportTypes)
            {
                if (importType == expT.Contract)
                {
                    return Activator.CreateInstance(expT.Type);
                }
            }
            return null;
        }

		public T CreateInstance<T>()
		{
            var type = this.CreateInstance(typeof(T));
            if (type != null)
            {
                return (T)type;
            }
            return default(T);
		}


		public static Customer Sample()
		{
			var container = new Container();
			container.AddAssembly(Assembly.GetExecutingAssembly());

            container.AddType(typeof(CustomerDAL), typeof(ICustomerDAL));
            container.AddType(typeof(CustomerBLL));
            container.AddType(typeof(Logger));

            var customerBLL = (CustomerBLL)container.CreateInstance(typeof(CustomerBLL));
			var customerBLL2 = container.CreateInstance<CustomerBLL2>();

            return customerBLL2;
        }

        [TestClass]
        public class Test
        {
            [TestMethod]
            public void TestNotNull()
            {
                Assert.IsNotNull(Container.Sample());
            }

        }
	}
}
