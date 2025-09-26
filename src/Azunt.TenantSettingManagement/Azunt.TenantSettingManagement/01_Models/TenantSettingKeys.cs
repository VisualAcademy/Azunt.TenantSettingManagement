namespace Azunt.TenantSettingManagement;

/// <summary>
/// Provides strongly-typed constant keys for tenant settings.
/// 
/// ����:
/// - ���ڿ� Ű�� �ڵ� �������� ���� ���� ��� �� �������� �����Ͽ�
///   ��Ÿ(Typo)�� ����ġ�� ����.
/// - �˻�/�ڵ��ϼ����� ���� ã�� �� �ֵ��� Discoverability Ȯ��.
/// - ��ɺ��� Nested static class�� �ξ� ���� Ű���� �׷�ȭ.
/// 
/// Ȯ�� ���:
/// - ���ο� ����̳� ��ɿ��� ������ �ʿ��ϴٸ�
///   ��� �̸��� ������ ��ø Ŭ������ ����� �� �ȿ� Ű�� ����.
/// - ��: TenantSettingKeys.Dashboard.EnabledWidgets
/// </summary>
public static class TenantSettingKeys
{
    /// <summary>
    /// Keys under the "EmployeeSummary" feature area.
    /// Used to control visibility and feature toggles for the Employee Summary view component.
    /// 
    /// ���� ���:
    /// - ���� ��� ���� ǥ�� ����
    /// - HR ���ε� �� ǥ�� ����
    /// - �̸���/�������� �� ǥ�� ����
    /// 
    /// ����:
    /// - DB���� ���ڿ�("true"/"false")�� ����Ǹ�,
    ///   ���ø����̼� �ڵ忡�� bool ��ȯ �� ����ؾ� ��.
    /// </summary>
    public static class EmployeeSummary
    {
        /// <summary>
        /// Master toggle: ��ü Employee Summary ���� ���� ����.
        /// </summary>
        public const string Enabled = "EmployeeSummary:Enabled";

        /// <summary>
        /// HR Upload ��(���ε� ����/��¥) ǥ�� ����.
        /// </summary>
        public const string ShowHrUpload = "EmployeeSummary:ShowHrUpload";

        /// <summary>
        /// ���� Email �� ǥ�� ����.
        /// </summary>
        public const string ShowEmail = "EmployeeSummary:ShowEmail";

        /// <summary>
        /// ���� CreatedAt(��������) �� ǥ�� ����.
        /// </summary>
        public const string ShowCreatedAt = "EmployeeSummary:ShowCreatedAt";
    }
}
