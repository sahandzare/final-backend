using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Camping.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OwnerController : Controller
    {
        public readonly string _connectionString = "server=127.0.0.1;database=camping;uid=root;pwd=root";

        [HttpPost("login")]
        public IActionResult OwnerLogin([FromBody] Owner owner)
        {
            try
            {
              
                bool isAuthenticated = AuthenticateOwner(owner.Username, owner.Password);
                if (!isAuthenticated)
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }

               
                return Ok(new { message = "Owner authenticated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }


        [HttpGet("spots")]
        public IActionResult GetAllSpots()
        {
            try
            {
               
                List<CampingSpot> spots = GetAllCampingSpotsFromDatabase();

                return Ok(spots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("addspot")]
        public IActionResult AddCampingSpot([FromBody] CampingSpot campingSpot)
        {
            try
            {
                
               
                int campingSpotId = InsertCampingSpotIntoDatabase(campingSpot);

                
                return Ok(new { message = "Camping spot added successfully.", campingSpotId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        private bool AuthenticateOwner(string username, string password)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM owner WHERE Username = @Username AND Password = @Password";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private List<CampingSpot> GetAllCampingSpotsFromDatabase()
        {
            List<CampingSpot> spots = new List<CampingSpot>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id,Name, Location, Description, Price FROM spots";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            spots.Add(new CampingSpot
                            {
                                Name = reader.GetString("Name"),
                                Location = reader.GetString("Location"),
                                Description = reader.GetString("Description"),
                                Price = reader.GetDecimal("Price"),
                                Id = reader.GetInt32("Id"),
                            }) ;
                        }
                    }
                }
            }

            return spots;
        }

        private int InsertCampingSpotIntoDatabase(CampingSpot campingSpot)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "INSERT INTO spots (Name, Location, Description, Price,Availability) VALUES (@Name, @Location, @Description, @Price,@Availability)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", campingSpot.Name);
                    command.Parameters.AddWithValue("@Location", campingSpot.Location);
                    command.Parameters.AddWithValue("@Description", campingSpot.Description);
                    command.Parameters.AddWithValue("@Price", campingSpot.Price);
                    command.Parameters.AddWithValue("@Availability", campingSpot.Availability);

                    int campingSpotId = Convert.ToInt32(command.ExecuteScalar());
                    return campingSpotId;
                }
            }
        }
    }
}
