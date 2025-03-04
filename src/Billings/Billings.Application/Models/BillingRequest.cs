﻿using Library.Results;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Billings.Application.Models
{
    public class BillingRequest : IRequest<IResult>
    {
        [Required] public string Cpf { get; set; }
        [Required] public string DueDate { get; set; }
        [Required] public double Amount { get; set; }
    }
}
