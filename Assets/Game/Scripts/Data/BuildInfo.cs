// Seule source de verite pour la version du build remontee dans
// trial_responses.build_version (contrat : docs/payload-examples.md cote
// backend-bugs). A incrementer ici, et nulle part ailleurs.
public static class BuildInfo
{
    // Bump a chaque build livree dans backend-bugs/public/unity/Build — minor quand le
    // contrat trial_responses change, patch sinon. Convention : backend-bugs/AGENTS.md.
    public const string Version = "1.3.1";
}
