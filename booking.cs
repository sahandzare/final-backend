namespace Camping
{
    public class booking
    {
        public int UserId { get; set; }

       public int CampingSpotId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime BookingDate { get; set; }
        public int BookingId { get; set; }

    }
}
