public class NClass
{
	public int classId;

	public string name;

	public SkillTemplate[] skillTemplates;

    public SkillTemplate[] availableSkillTemplates
    {
        get { return System.Array.FindAll(skillTemplates, t => t != null && !Skill.isDisabled(t.id)); }
    }
}
