using Microsoft.AspNetCore.Identity;

namespace CCMobile.Infrastructure
{
    public class VietnameseIdentityErrorDescriber
        : IdentityErrorDescriber
    {
        private static IdentityError Error(
            string code, string description)
        {
            return new IdentityError
            {
                Code = code,
                Description = description
            };
        }

        public override IdentityError DefaultError()
            => Error(nameof(DefaultError),
                "Đã xảy ra lỗi. Vui lòng thử lại.");

        public override IdentityError DuplicateEmail(string email)
            => Error(nameof(DuplicateEmail),
                "Email này đã được đăng ký.");

        public override IdentityError DuplicateUserName(string userName)
            => Error(nameof(DuplicateUserName),
                "Tên đăng nhập hoặc email này đã được sử dụng.");

        public override IdentityError InvalidEmail(string? email)
            => Error(nameof(InvalidEmail),
                "Địa chỉ email không hợp lệ.");

        public override IdentityError InvalidUserName(string? userName)
            => Error(nameof(InvalidUserName),
                "Tên đăng nhập không hợp lệ. Vui lòng kiểm tra email.");

        public override IdentityError PasswordTooShort(int length)
            => Error(nameof(PasswordTooShort),
                $"Mật khẩu phải có ít nhất {length} ký tự.");

        public override IdentityError PasswordRequiresDigit()
            => Error(nameof(PasswordRequiresDigit),
                "Mật khẩu phải có ít nhất một chữ số (0–9).");

        public override IdentityError PasswordRequiresLower()
            => Error(nameof(PasswordRequiresLower),
                "Mật khẩu phải có ít nhất một chữ thường (a–z).");

        public override IdentityError PasswordRequiresUpper()
            => Error(nameof(PasswordRequiresUpper),
                "Mật khẩu phải có ít nhất một chữ hoa (A–Z).");

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => Error(nameof(PasswordRequiresNonAlphanumeric),
                "Mật khẩu phải có ít nhất một ký tự đặc biệt.");

        public override IdentityError PasswordRequiresUniqueChars(
            int uniqueChars)
            => Error(nameof(PasswordRequiresUniqueChars),
                $"Mật khẩu phải có ít nhất {uniqueChars} ký tự khác nhau.");

        public override IdentityError PasswordMismatch()
            => Error(nameof(PasswordMismatch),
                "Mật khẩu không chính xác.");

        public override IdentityError InvalidToken()
            => Error(nameof(InvalidToken),
                "Mã xác nhận không hợp lệ hoặc đã hết hạn.");

        public override IdentityError ConcurrencyFailure()
            => Error(nameof(ConcurrencyFailure),
                "Dữ liệu đã thay đổi. Vui lòng tải lại trang và thử lại.");
    }
}