﻿using Contact.Api.Enums;

namespace Contact.Api.Daos
{
    /// <summary>
    /// Represents a contact information DAO for a contact.
    /// </summary>
    /// <remarks>This DAO does not have a contact ID property. See <see cref="ContactInformationDao"/> for the DAO with the ID property.</remarks>
    public class ContactInformationWithoutContactIdDao
    {
        /// <summary>
        /// The type of the contact information.
        /// </summary>
        public ContactInformationType Type { get; set; }

        /// <summary>
        /// The content of the contact information.
        /// </summary>
        public string? Content { get; set; }
    }
}
