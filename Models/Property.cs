using System;
using System.Collections.Generic;
using System.Text;

namespace Collection_Management.Models
{
    public enum PropertyType
    {
        String,
        Number,
        Enum
    }
    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public PropertyType Type { get; set; }

        public string DisplayValue => $"{Name}: {Value}";

        public Property(string name, PropertyType type)
        {
            Name = name;
            Type = type;

            switch(type)
            {
                case PropertyType.String:
                {
                    Value = "";
                    break;
                }
                case PropertyType.Number:
                {
                    Value = "0";
                    break;
                } 
                case PropertyType.Enum: 
                {
                    Value = null;
                    break;
                }
                default: {
                    Value = null;
                    break;
                } 
            };
        }
    }
}
