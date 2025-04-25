namespace WeddingShare.Constants
{
    public class ViewOptions
    {
        public static IDictionary<string, string> YesNo = new Dictionary<string, string>()
        {
            { "Yes", "true" },
            { "No", "false" }
        };

        public static IDictionary<string, string> YesNoInverted = new Dictionary<string, string>()
        {
            { "Yes", "false" },
            { "No", "true" }
        };

        public static IDictionary<string, string> SingleGalleryMode = new Dictionary<string, string>()
        {
            { "Single", "true" },
            { "Multiple", "false" }
        };

        public static IDictionary<string, string> GallerySelectorDropdown = new Dictionary<string, string>()
        {
            { "Dropdown", "true" },
            { "Input", "false" }
        };

        public static IDictionary<string, string> GalleryWidth = new Dictionary<string, string>()
        {
            { "Full Width", "true" },
            { "Default", "false" }
        };

        public static IDictionary<string, string> GalleryDefaultView = new Dictionary<string, string>()
        {
            { "Default", "default" },
            { "Presentation", "presentation" },
            { "Slideshow", "slideshow" }
        };

        public static IDictionary<string, string> GalleryDefaultSort = new Dictionary<string, string>()
        {
            { "Ascending", "0" },
            { "Descending", "1" },
            { "Random", "2" }
        };
    }
}