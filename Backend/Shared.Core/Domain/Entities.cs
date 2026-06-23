using System;

namespace Shared.Core.Domain
{
    public class Employee
    {
        public string EmpID { get; set; } = string.Empty;
        public string EmpName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string emailid { get; set; } = string.Empty;
        public string mobile { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string EmpType { get; set; } = "contractor";
        public bool IsMfaEnabled { get; set; } = true;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public bool IsAdmin { get; set; } = false;
    }

    public class Book
    {
        public long Anum { get; set; }
        public string Book_name { get; set; } = string.Empty;
        public string Book_author { get; set; } = string.Empty;
        public string Book_rack { get; set; } = string.Empty;
        public string Book_class { get; set; } = string.Empty;
        public string Book_category { get; set; } = string.Empty;
        public bool Available { get; set; }
        public string IssuedTo { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public int TotalCopies { get; set; } = 1;
        public string CoverImagePath { get; set; } = string.Empty;
    }

    public class Magazine
    {
        public long MagazineId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string IssueDate { get; set; } = string.Empty;
        public string RackLocation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalCopies { get; set; } = 1;
        public int AvailableCopies { get; set; } = 1;
        public string CoverImagePath { get; set; } = string.Empty;
    }

    public class Issue
    {
        public int IssueNumber { get; set; }
        public long Anum { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string EmpID { get; set; } = string.Empty;
        public string EmpName { get; set; } = string.Empty;
        public string IssueDate { get; set; } = string.Empty;
        public bool ISemp { get; set; } = true;
        public DateTime? DueDate { get; set; }
        public int ReissueCount { get; set; } = 0;
        public string ItemType { get; set; } = "Book";
        
        // Not mapped to DB
        public string CoverImagePath { get; set; } = string.Empty;
        public string EmployeeImagePath { get; set; } = string.Empty;
    }

    public class IssueHistory
    {
        public int IssueNumber { get; set; }
        public long Anum { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string EmpID { get; set; } = string.Empty;
        public string EmpName { get; set; } = string.Empty;
        public string IssueDate { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public bool ISemp { get; set; } = true;
        public string ItemType { get; set; } = "Book";
        
        // Not mapped to DB
        public string CoverImagePath { get; set; } = string.Empty;
        public string EmployeeImagePath { get; set; } = string.Empty;
    }

    public class IssueRequest
    {
        public int RequestID { get; set; }
        public string EmpID { get; set; } = string.Empty;
        public string ItemType { get; set; } = "Book";
        public long ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        
        // Extended info for UI
        public string EmpName { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public string EmployeeImagePath { get; set; } = string.Empty;
    }

    public class IssueRequestDto
    {
        public string EmpID { get; set; } = string.Empty;
        public string ItemType { get; set; } = "Book";
        public long ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
    }

    public class ExecutionPlan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
    }

    public class Reservation
    {
        public int Id { get; set; }
        public long Anum { get; set; }
        public string EmpID { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ItemType { get; set; } = "Book";
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EmpID { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class EmailTemplate
    {
        public int TemplateId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class UserRefreshToken
    {
        public int Id { get; set; }
        public string EmpID { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expiry;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
