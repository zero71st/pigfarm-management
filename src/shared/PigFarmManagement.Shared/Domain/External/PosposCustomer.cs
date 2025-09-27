using System.Text.Json.Serialization;

namespace PigFarmManagement.Shared.Models.External;

public class PosposCustomer
{
    [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("phonenumber")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("tags")] public List<PosposTag> Tags { get; set; } = new();
    [JsonPropertyName("province_other")] public string ProvinceOther { get; set; } = string.Empty;
    [JsonPropertyName("storename")] public string StoreName { get; set; } = string.Empty;
    [JsonPropertyName("id_card")] public string? IdCard { get; set; }
    [JsonPropertyName("taxid")] public string TaxId { get; set; } = string.Empty;
    [JsonPropertyName("branch")] public string Branch { get; set; } = string.Empty;
    [JsonPropertyName("status")] public bool Status { get; set; }
    [JsonPropertyName("member_credit_status")] public bool MemberCreditStatus { get; set; }
    [JsonPropertyName("member_credit")] public int MemberCredit { get; set; }
    [JsonPropertyName("company_email")] public string CompanyEmail { get; set; } = string.Empty;
    [JsonPropertyName("company_phonenumber")] public string CompanyPhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("company_province_other")] public string CompanyProvinceOther { get; set; } = string.Empty;
    [JsonPropertyName("company_zipcode")] public string CompanyZipcode { get; set; } = string.Empty;
    [JsonPropertyName("price_level")] public string PriceLevel { get; set; } = string.Empty;
    [JsonPropertyName("stamp_value")] public int StampValue { get; set; }
    [JsonPropertyName("point_value")] public int PointValue { get; set; }
    [JsonPropertyName("address_status")] public bool AddressStatus { get; set; }
    [JsonPropertyName("create_date")] public DateTimeOffset? CreateDate { get; set; }
    [JsonPropertyName("favorite")] public bool Favorite { get; set; }
    [JsonPropertyName("type")] public PosposType? Type { get; set; }
    [JsonPropertyName("firstname")] public string FirstName { get; set; } = string.Empty;
    [JsonPropertyName("lastname")] public string LastName { get; set; } = string.Empty;
    [JsonPropertyName("sex")] public string Sex { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("line_id")] public string? LineId { get; set; }
    [JsonPropertyName("key_card_id")] public string KeyCardId { get; set; } = string.Empty;
    [JsonPropertyName("address")] public string Address { get; set; } = string.Empty;
    [JsonPropertyName("country")] public PosposCountry? Country { get; set; }
    [JsonPropertyName("province")] public PosposProvince? Province { get; set; }
    [JsonPropertyName("district")] public PosposDistrict? District { get; set; }
    [JsonPropertyName("zipcode")] public string Zipcode { get; set; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("point_history")] public List<object> PointHistory { get; set; } = new();
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
}

public class PosposTag { [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("name")] public string Name { get; set; } = string.Empty; }
public class PosposType { [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("name_th")] public string NameTh { get; set; } = string.Empty; [JsonPropertyName("name_en")] public string NameEn { get; set; } = string.Empty; }
public class PosposCountry { [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("COUNTRY_NAME_TH")] public string CountryNameTh { get; set; } = string.Empty; [JsonPropertyName("ABBREVIATION")] public string Abbreviation { get; set; } = string.Empty; [JsonPropertyName("COUNTRY_NAME_EN")] public string CountryNameEn { get; set; } = string.Empty; [JsonPropertyName("COUNTRY_NAME")] public string CountryName { get; set; } = string.Empty; }
public class PosposProvince { [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("PROVINCE_NAME_TH")] public string ProvinceNameTh { get; set; } = string.Empty; [JsonPropertyName("PROVINCE_NAME_EN")] public string ProvinceNameEn { get; set; } = string.Empty; [JsonPropertyName("COUNTRY_ID")] public string CountryId { get; set; } = string.Empty; [JsonPropertyName("GEOGRAPHIC_CODE")] public int GeographicCode { get; set; } }
public class PosposDistrict { [JsonPropertyName("IS_DELETED")] public bool IsDeleted { get; set; } [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("PROVINCE_ID")] public string ProvinceId { get; set; } = string.Empty; [JsonPropertyName("DISTRICT_NAME_TH")] public string DistrictNameTh { get; set; } = string.Empty; [JsonPropertyName("DISTRICT_NAME_EN")] public string DistrictNameEn { get; set; } = string.Empty; [JsonPropertyName("GEOGRAPHIC_CODE")] public int GeographicCode { get; set; } }
