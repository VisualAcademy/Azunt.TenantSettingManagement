namespace Azunt.TenantSettingManagement;

/// <summary>
/// Provides strongly-typed constant keys for tenant settings.
/// 
/// 목적:
/// - 문자열 키를 코드 곳곳에서 직접 쓰는 대신 한 곳에서만 정의하여
///   오타(Typo)나 불일치를 방지.
/// - 검색/자동완성으로 쉽게 찾을 수 있도록 Discoverability 확보.
/// - 기능별로 Nested static class를 두어 관련 키들을 그룹화.
/// 
/// 확장 방법:
/// - 새로운 모듈이나 기능에서 설정이 필요하다면
///   모듈 이름과 동일한 중첩 클래스를 만들고 그 안에 키를 정의.
/// - 예: TenantSettingKeys.Dashboard.EnabledWidgets
/// </summary>
public static class TenantSettingKeys
{
    /// <summary>
    /// Keys under the "EmployeeSummary" feature area.
    /// Used to control visibility and feature toggles for the Employee Summary view component.
    /// 
    /// 관련 기능:
    /// - 직원 목록 위젯 표시 여부
    /// - HR 업로드 열 표시 여부
    /// - 이메일/생성일자 열 표시 여부
    /// 
    /// 주의:
    /// - DB에는 문자열("true"/"false")로 저장되며,
    ///   애플리케이션 코드에서 bool 변환 후 사용해야 함.
    /// </summary>
    public static class EmployeeSummary
    {
        /// <summary>
        /// Master toggle: 전체 Employee Summary 위젯 노출 여부.
        /// </summary>
        public const string Enabled = "EmployeeSummary:Enabled";

        /// <summary>
        /// HR Upload 열(업로드 상태/날짜) 표시 여부.
        /// </summary>
        public const string ShowHrUpload = "EmployeeSummary:ShowHrUpload";

        /// <summary>
        /// 직원 Email 열 표시 여부.
        /// </summary>
        public const string ShowEmail = "EmployeeSummary:ShowEmail";

        /// <summary>
        /// 직원 CreatedAt(생성일자) 열 표시 여부.
        /// </summary>
        public const string ShowCreatedAt = "EmployeeSummary:ShowCreatedAt";
    }
}
