using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;

namespace Camping.Controllers
{


    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        public readonly string _connectionString = "server=127.0.0.1;database=camping;uid=root;pwd=root";

        [HttpPost("signup")]


        public IActionResult Signup(User user)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Users(Username,Email,Password) VALUES(@Username,@Email,@Password)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@Email", user.Email);
                        command.Parameters.AddWithValue("@Password", user.Password);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok("User registered successfully!");
                        }
                        else
                        {
                            return BadRequest("Failed to register user.");
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("login")]
        public IActionResult Login(LoginRequest loginRequest)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", loginRequest.Username);
                        command.Parameters.AddWithValue("@Password", loginRequest.Password);

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        if (count > 0)
                        {
                            return Ok("Login successful!");
                        }
                        else
                        {
                            return Unauthorized("Invalid username or password.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("update")]
       
        public IActionResult UpdateUserDetails([FromBody] UserUpdateRequest userUpdateRequest)
        {
            try
            {
               
                if (!UserExists(userUpdateRequest.CurrentUsername, userUpdateRequest.CurrentPassword))
                {
                    return NotFound("User not found or incorrect username/password.");
                }

               
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "UPDATE users SET ";

                   
                    List<string> updateFields = new List<string>();
                    if (!string.IsNullOrEmpty(userUpdateRequest.NewEmail))
                    {
                        updateFields.Add("Email = @Email");
                    }
                    if (!string.IsNullOrEmpty(userUpdateRequest.NewUsername))
                    {
                        updateFields.Add("Username = @NewUsername");
                    }
                    if (!string.IsNullOrEmpty(userUpdateRequest.NewPassword))
                    {
                        updateFields.Add("Password = @NewPassword");
                    }

                    if (updateFields.Count == 0)
                    {
                        return BadRequest("No fields provided for update.");
                    }

                    query += string.Join(", ", updateFields) + " WHERE Username = @Username";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(userUpdateRequest.NewEmail))
                        {
                            command.Parameters.AddWithValue("@Email", userUpdateRequest.NewEmail);
                        }
                        if (!string.IsNullOrEmpty(userUpdateRequest.NewUsername))
                        {
                            command.Parameters.AddWithValue("@NewUsername", userUpdateRequest.NewUsername);
                        }
                        if (!string.IsNullOrEmpty(userUpdateRequest.NewPassword))
                        {
                            command.Parameters.AddWithValue("@NewPassword", userUpdateRequest.NewPassword);
                        }

                        command.Parameters.AddWithValue("@Username", userUpdateRequest.CurrentUsername);

                       
                        int rowsAffected = command.ExecuteNonQuery();

                       
                        if (rowsAffected > 0)
                        {
                            return Ok("User details updated successfully.");
                        }
                        else
                        {
                            return NotFound($"User '{userUpdateRequest.CurrentUsername}' not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool UserExists(string username, string password)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM users WHERE Username = @Username AND Password = @Password";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

    }
}

