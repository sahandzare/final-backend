using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Routing;

namespace Camping
{
    public class CampingSpot

    {
        
        public int Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }
        public string Location { get; set; }

        public decimal Price { get; set; }

        public int Availability { get; set; }

        

    }
}
