namespace TaskFlow.Domain.Projects;

public sealed class MemberRole : Common.ValueObject
{
    
    // 使える値を静的に定義（外部はこれを使う）
    public static readonly MemberRole Owner = new ("Owner");
    public static readonly MemberRole Admin = new ("Admin");
    public static readonly MemberRole Member = new ("Member");
    
    public string Value { get; }

    // privateにして外から new できないようにする
    private MemberRole(string value)
    {
        Value = value;
    }

    public static MemberRole FromValue(string value) => value switch
    {
        "Owner"  => Owner,
        "Admin"  => Admin,
        "Member" => Member,
        _ => throw new ArgumentException($"Invalid MemberRole: {value}")
    };

    // ValueObjectの抽象メソッドを実装
    // → 同一性の基準はValueだけ
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
