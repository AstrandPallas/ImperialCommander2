using System.Collections.Generic;

namespace Saga
{
	public class CampaignHero
	{
		public string heroID;
		public string mugShotPath;
		public List<CampaignItem> campaignItems = new List<CampaignItem>();
		public List<CampaignSkill> campaignSkills = new List<CampaignSkill>();
		public int xpAmount;

		// What the campaign has added to the printed hero sheet. Rewards,
		// items and class cards raise these and the gains persist between
		// missions, so the tracker would otherwise reset a hero to their
		// starting numbers at the top of every mission.
		//
		// Bonuses rather than absolute values: zero means "whatever the sheet
		// says", so a corrected sheet flows through without anybody re-entering
		// a number, and an older save with these fields absent loads as a hero
		// with no gains rather than as a hero with no health.
		public int bonusHealth;
		public int bonusEndurance;
		public int bonusSpeed;
		public string bonusReason = "";
	}
}
