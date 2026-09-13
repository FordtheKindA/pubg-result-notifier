public class PubgMatchResponse
{
    public PubgMatchData Data { get; set; }
    public List<PubgIncluded> Included { get; set; }
}

public class PubgMatchData
{
    public string Id { get; set; } = "";
    public PubgMatchAttributes Attributes { get; set; }
}

public class PubgMatchAttributes
{
    public string MapName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsCustomMatch { get; set; }
}

public class PubgIncluded
{
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public PubgIncludedAttributes? Attributes { get; set; }
    public PubgIncludedRelationships? Relationships { get; set; }
}

public class PubgIncludedAttributes
{
    public PubgIncludedStats? Stats { get; set; }
}

public class PubgIncludedStats
{
    public int? Rank { get; set; }
    public int? TeamId { get; set; }

    public string? Name { get; set; }
    public int? Kills { get; set; }
}

public class PubgIncludedRelationships
{
    public PubgParticipantsRelation? Participants { get; set; }
}

public class PubgParticipantsRelation
{
    public List<PubgRelationData> Data { get; set; }
}

public class PubgRelationData
{
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
}