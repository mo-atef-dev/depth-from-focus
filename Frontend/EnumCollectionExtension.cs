using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Focus_Measurement_Tool
{
    public class EnumCollectionExtension : MarkupExtension
    {
        public Type EnumType { get; set; }

        public override object ProvideValue(IServiceProvider _)
        {
            if (EnumType != null)
            {
                return CreateEnumValueList(EnumType);
            }
            return default(object);
        }

        private List<object> CreateEnumValueList(Type enumType)
        {
            return Enum.GetNames(enumType)
                .Select(name => Enum.Parse(enumType, name))
                .ToList();
        }
    }
}
