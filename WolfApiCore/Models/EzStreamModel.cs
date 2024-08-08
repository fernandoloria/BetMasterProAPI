namespace WolfApiCore.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EzStreamModel
    {
        public Dictionary<string, EzSportEvents> Sports { get; set; } = new Dictionary<string, EzSportEvents>();

        public bool Error { get; set; }

        public string Error_Explain { get; set; }

        public long Modified_Time { get; set; }
    }

    public class EzSportEvents
    {
        [JsonConverter(typeof(SportEventsConverter))]
        public Dictionary<string, EzEvent> Events { get; set; } = new Dictionary<string, EzEvent>();

        public int Count { get; set; }
    }

    public class EzEvent
    {
        public string Sport { get; set; }

        public string League { get; set; }

        public EzCompetitors competitiors { get; set; }

        public int Stream_Id { get; set; }

        public long Feed_Id { get; set; }

        public string Donbest_Id { get; set; }

        public List<object> Donbest_Id_Multi { get; set; }
    }

    public class EzCompetitors
    {
        public string Home { get; set; }
        public string Away { get; set; }
    }


    /* Manejar la conversion del SportsEvent */
    public class SportEventsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, EzEvent>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Only reading JSON is supported.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return new Dictionary<string, EzEvent>();
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                // Handle an empty array or array with items as a Dictionary
                var jsonArray = JArray.Load(reader);
                var events = new Dictionary<string, EzEvent>();
                foreach (var item in jsonArray)
                {
                    // If the items in the array are objects with IDs, you can process them
                    var eventObj = item.ToObject<EzEvent>();
                    // Add a dummy key here if necessary, adjust based on your actual data structure
                    events.Add(Guid.NewGuid().ToString(), eventObj);
                }
                return events;
            }

            // Handle cases where JSON is an object
            var jsonObject = JObject.Load(reader);
            return jsonObject.ToObject<Dictionary<string, EzEvent>>();
        }
    }
}
