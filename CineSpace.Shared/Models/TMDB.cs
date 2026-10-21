using System.Collections.Generic;

namespace CineSpace.Shared.Models
{
    public class TMDBResponse
    {
        public List<TMDBMovie> results { get; set; } = new List<TMDBMovie>();
        public int total_results { get; set; }
        public int total_pages { get; set; }
    }

    public class TMDBMovie
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? poster_path { get; set; }
        public string backdrop_path { get; set; } = "";
        public string overview { get; set; } = "";
        public string release_date { get; set; } = "";
        public double vote_average { get; set; }
        public List<int> genre_ids { get; set; } = [];
    }
}
