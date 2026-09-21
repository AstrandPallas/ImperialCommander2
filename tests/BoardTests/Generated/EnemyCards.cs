// GENERATED from ImperialCommander2/Assets/Resources/CardData/enemies.json
// Regenerate with: python tools/gen_enemy_fixture.py
// FFG's own card data, so the mapping is exercised against every unit that
// actually ships rather than against hand-written examples.
namespace Saga.Board.Tests
{
	public sealed class EnemyCard
	{
		public string Name;
		public string Id;
		public string AttackType;
		public string MiniSize;
		public int Speed;
		public string[] Keywords;
	}

	public static class EnemyCards
	{
		public static readonly EnemyCard[] All = new EnemyCard[]
		{
			new EnemyCard { Name = "Stormtrooper", Id = "DG001", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Stormtrooper", Id = "DG002", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Stormtrooper (Elite)", Id = "DG003", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Imperial Officer", Id = "DG004", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Imperial Officer", Id = "DG005", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Imperial Officer (Elite)", Id = "DG006", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "E-Web Engineer", Id = "DG007", AttackType = "Ranged", MiniSize = "Medium1x2", Speed = 2, Keywords = new string[] { "+3 Accuracy" } },
			new EnemyCard { Name = "E-Web Engineer (Elite)", Id = "DG008", AttackType = "Ranged", MiniSize = "Medium1x2", Speed = 3, Keywords = new string[] { "+1 {G}", "+3 Accuracy" } },
			new EnemyCard { Name = "Royal Guard", Id = "DG009", AttackType = "Melee", MiniSize = "Small1x1", Speed = 5, Keywords = new string[] { "Reach" } },
			new EnemyCard { Name = "Royal Guard (Elite)", Id = "DG010", AttackType = "Melee", MiniSize = "Small1x1", Speed = 5, Keywords = new string[] { "Reach", "+1 {F}" } },
			new EnemyCard { Name = "Probe Droid", Id = "DG011", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 3, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "Probe Droid", Id = "DG012", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 3, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "Probe Droid (Elite)", Id = "DG013", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "AT-ST", Id = "DG014", AttackType = "Ranged", MiniSize = "Huge2x3", Speed = 4, Keywords = new string[] { "Massive", "+3 Accuracy" } },
			new EnemyCard { Name = "Trandoshan Hunter", Id = "DG015", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Trandoshan Hunter (Elite)", Id = "DG016", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Nexu", Id = "DG017", AttackType = "Melee", MiniSize = "Large2x2", Speed = 6, Keywords = new string[] { "Mobile", "Bleed" } },
			new EnemyCard { Name = "Nexu (Elite)", Id = "DG018", AttackType = "Melee", MiniSize = "Large2x2", Speed = 6, Keywords = new string[] { "Mobile", "Bleed", "Cleave 2 {H}" } },
			new EnemyCard { Name = "Heavy Stormtrooper", Id = "DG019", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 3, Keywords = new string[] { "+2 Accuracy" } },
			new EnemyCard { Name = "Heavy Stormtrooper (Elite)", Id = "DG020", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 3, Keywords = new string[] { "+2 Accuracy" } },
			new EnemyCard { Name = "Tusken Raider", Id = "DG021", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Habitat: Desert" } },
			new EnemyCard { Name = "Tusken Raider (Elite)", Id = "DG022", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+1 {H}", "Habitat: Desert" } },
			new EnemyCard { Name = "Snowtrooper", Id = "DG023", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Snowtrooper (Elite)", Id = "DG024", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "SC2-M Repulsor Tank", Id = "DG025", AttackType = "Ranged", MiniSize = "Huge2x3", Speed = 4, Keywords = new string[] { "Massive", "+2 Accuracy" } },
			new EnemyCard { Name = "HK Assassin Droid", Id = "DG026", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "HK Assassin Droid (Elite)", Id = "DG027", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Wampa", Id = "DG028", AttackType = "Melee", MiniSize = "Medium1x2", Speed = 3, Keywords = new string[] { "+1 {H}", "Habitat: Snow" } },
			new EnemyCard { Name = "Wampa (Elite)", Id = "DG029", AttackType = "Melee", MiniSize = "Medium1x2", Speed = 3, Keywords = new string[] { "+2 {H}", "Habitat: Snow" } },
			new EnemyCard { Name = "Ugnaught Tinkerer", Id = "DG030", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Ugnaught Tinkerer", Id = "DG031", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Ugnaught Tinkerer (Elite)", Id = "DG032", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Wing Guard", Id = "DG033", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Wing Guard", Id = "DG034", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Wing Guard (Elite)", Id = "DG035", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Jet Trooper", Id = "DG036", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "Jet Trooper", Id = "DG037", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "Jet Trooper (Elite)", Id = "DG038", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Mobile" } },
			new EnemyCard { Name = "Gamorrean Guard", Id = "DG039", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Reach", "-1 {H}" } },
			new EnemyCard { Name = "Gamorrean Guard", Id = "DG040", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Reach", "-1 {H}" } },
			new EnemyCard { Name = "Gamorrean Guard (Elite)", Id = "DG041", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Reach" } },
			new EnemyCard { Name = "Weequay Pirate", Id = "DG042", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+1 Accuracy" } },
			new EnemyCard { Name = "Weequay Pirate", Id = "DG043", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+1 Accuracy" } },
			new EnemyCard { Name = "Weequay Pirate (Elite)", Id = "DG044", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+2 Accuracy" } },
			new EnemyCard { Name = "Rancor", Id = "DG045", AttackType = "Melee", MiniSize = "Huge2x3", Speed = 4, Keywords = new string[] { "Massive", "Reach", "+1 {G}" } },
			new EnemyCard { Name = "AT-DP", Id = "DG046", AttackType = "Ranged", MiniSize = "Huge2x3", Speed = 3, Keywords = new string[] { "Massive", "+1 {G}", "+3 Accuracy" } },
			new EnemyCard { Name = "Riot Trooper", Id = "DG047", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Riot Trooper", Id = "DG048", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Riot Trooper (Elite)", Id = "DG049", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Weaken" } },
			new EnemyCard { Name = "Sentry Droid", Id = "DG050", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Sentry Droid", Id = "DG051", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Sentry Droid (Elite)", Id = "DG052", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Clawdite Shapeshifter", Id = "DG053", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Clawdite Shapeshifter", Id = "DG054", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Clawdite Shapeshifter (Elite)", Id = "DG055", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Death Trooper", Id = "DG056", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+3 Accuracy" } },
			new EnemyCard { Name = "Death Trooper", Id = "DG057", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+3 Accuracy" } },
			new EnemyCard { Name = "Death Trooper (Elite)", Id = "DG058", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+4 Accuracy" } },
			new EnemyCard { Name = "Death Trooper (Elite)", Id = "DG059", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+4 Accuracy" } },
			new EnemyCard { Name = "Loth-cat", Id = "DG060", AttackType = "Melee", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "Pierce 1" } },
			new EnemyCard { Name = "Loth-cat (Elite)", Id = "DG061", AttackType = "Melee", MiniSize = "Small1x1", Speed = 5, Keywords = new string[] { "Pierce 1" } },
			new EnemyCard { Name = "Dewback Rider (Elite)", Id = "DG062", AttackType = "Ranged", MiniSize = "Medium1x2", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "Hired Gun", Id = "DG063", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 5, Keywords = new string[] {  } },
			new EnemyCard { Name = "Hired Gun (Elite)", Id = "DG064", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 5, Keywords = new string[] { "+1 {F}", "+1 Accuracy" } },
			new EnemyCard { Name = "Bantha Rider (Elite)", Id = "DG065", AttackType = "Ranged", MiniSize = "Huge2x3", Speed = 5, Keywords = new string[] { "Massive", "Habitat: Desert" } },
			new EnemyCard { Name = "Jawa Scavenger", Id = "DG066", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+2 Accuracy" } },
			new EnemyCard { Name = "Jawa Scavenger (Elite)", Id = "DG067", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] { "+2 Accuracy" } },
			new EnemyCard { Name = "ISB Infiltrator", Id = "DG068", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
			new EnemyCard { Name = "ISB Infiltrator (Elite)", Id = "DG069", AttackType = "Ranged", MiniSize = "Small1x1", Speed = 4, Keywords = new string[] {  } },
		};
	}
}
