using ACadSharp.Attributes;
using System;
using System.Linq;
using System.Xml.Linq;

namespace ACadSharp.Objects
{
    /// <summary>
    /// Represents a <see cref="BookColor"/> object.
    /// </summary>
    /// <remarks>
    /// Object name <see cref="DxfFileToken.ObjectDBColor"/> <br/>
    /// Dxf class name <see cref="DxfSubclassMarker.DbColor"/>
    /// </remarks>
    [DxfName(DxfFileToken.ObjectDBColor)]
    [DxfSubClass(DxfSubclassMarker.DbColor)]
    public class BookColor : NonGraphicalObject
    {
        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.UNLISTED;

        /// <inheritdoc/>
        public override string ObjectName => DxfFileToken.ObjectDBColor;

        /// <inheritdoc/>
        public override string SubclassMarker => DxfSubclassMarker.DbColor;

        /// <summary>
        /// Color name.
        /// </summary>
        public string ColorName { get; internal set; }

        /// <summary>
        /// Book name where the color is stored.
        /// </summary>
        public string BookName { get; internal set; }

        [DxfCodeValue(62, 420)]
        public Color Color { get; set; }

        internal BookColor() : base(string.Empty)
        {
            BookName = string.Empty;
            ColorName = string.Empty;
        }
        
        public BookColor(string book_name, string color_name) : base(book_name + color_name)
        {
            BookName = book_name;
            ColorName = color_name;
        }
    }
}