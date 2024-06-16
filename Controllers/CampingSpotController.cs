using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Cors;

namespace Camping.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class CampingSpotController : ControllerBase
    {

        public readonly string _connectionString = "server=127.0.0.1;database=camping;uid=root;pwd=root";

        [HttpGet]
        public IActionResult GetAllCampingSpots()
        {
            try
            {
                List<CampingSpot> campingSpots = new List<CampingSpot>();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT Id, Name, Location, Description, Price,Availability FROM spots";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                campingSpots.Add(new CampingSpot
                                {
                                    Id = reader.GetInt32("Id"),
                                    Name = reader.GetString("Name"),
                                    Location = reader.GetString("Location"),
                                    Description = reader.GetString("Description"),
                                    Price = reader.GetDecimal("Price"),
                                    Availability = reader.GetInt32("Availability")
                                });;
                            }
                        }
                    }
                }

                return Ok(campingSpots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet("{id}")]
        public IActionResult GetCampingSpotById(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT Id, Name, Location, Description, Price FROM spots WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var campingSpot = new CampingSpot
                                {
                                    Id = reader.GetInt32("Id"),
                                    Name = reader.GetString("Name"),
                                    Location = reader.GetString("Location"),
                                    Description = reader.GetString("Description"),
                                    Price = reader.GetDecimal("Price")
                                };

                                return Ok(campingSpot);
                            }
                            else
                            {
                                return NotFound("Camping spot not found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("filter")]
        public IActionResult FilterCampingSpots([FromQuery] string location = "", [FromQuery] decimal minPrice = 0, [FromQuery] decimal maxPrice = 1000)
        {
            try
            {
                List<CampingSpot> campingSpots = new List<CampingSpot>();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT Id, Name, Location, Description, Price FROM spots WHERE Price >= @minPrice AND Price <= @maxPrice";

                    
                    if (!string.IsNullOrWhiteSpace(location))
                    { //lowering the words 
                        query += " AND LOWER(Location) LIKE LOWER(@location)";
                    }
                    query += " AND Price >= @minPrice AND Price <= @maxPrice";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@minPrice", minPrice);
                        command.Parameters.AddWithValue("@maxPrice", maxPrice);

                        // added regex to find any word like the input
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            command.Parameters.AddWithValue("@location", $"%{location.ToLower()}%");
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                campingSpots.Add(new CampingSpot
                                {
                                    Id = reader.GetInt32("Id"),
                                    Name = reader.GetString("Name"),
                                    Location = reader.GetString("Location"),
                                    Description = reader.GetString("Description"),
                                    Price = reader.GetDecimal("Price")
                                });
                            }
                        }
                    }
                }
                if (campingSpots.Count == 0)
                {
                   
                        return NotFound($"Sorry, there are no spots in the location '{location}'.");
                    
                }


                return Ok(campingSpots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}