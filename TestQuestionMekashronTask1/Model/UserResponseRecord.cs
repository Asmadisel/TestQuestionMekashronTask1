namespace TestQuestionMekashronTask1.Model
{
    public class UserResponseRecord
    {
        public record UserResponse(
        int EntityId,
        string FirstName,
        string LastName,
        string Company,
        string Address,
        string City,
        string Country,
        string Zip,
        string Phone,
        string Mobile,
        string Email,
        int EmailConfirm,
        int MobileConfirm,
        int CountryID,
        int Status,
        string Lid,
        string FTPHost,
        int FTPPort
        );
    }
}
