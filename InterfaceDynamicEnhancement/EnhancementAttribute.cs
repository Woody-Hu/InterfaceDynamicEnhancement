using System;
using System.Collections.Generic;
using System.Text;

namespace InterfaceDynamicEnhancement
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EnhancementAttribute:Attribute
    {
        public string InterfaceName { get; }
    }
}
