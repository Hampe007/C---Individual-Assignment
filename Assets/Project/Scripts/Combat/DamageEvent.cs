public struct DamageEvent
{
    public int EnemyIndex;
    public int Damage;

    public DamageEvent(int enemyIndex, int damage)
    {
        EnemyIndex = enemyIndex;
        Damage = damage;
    }
}