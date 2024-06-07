using Newtonsoft.Json;

namespace Bott.Model
{
    public class BookItem
    {
        public string Title { get; set; }
        public List<string> Authors { get; set; }
    }

    public class GoogleBooksResponse
    {
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        public VolumeInfo VolumeInfo { get; set; }
    }

    public class VolumeInfo
    {
        public string Title { get; set; }
        public List<string> Authors { get; set; }
    }
    public class ReviewRequest
    {
        public string BookName { get; set; }
        public int Rating { get; set; }

        [JsonProperty("books")]
        public List<string> Books { get; set; }

    }

}
