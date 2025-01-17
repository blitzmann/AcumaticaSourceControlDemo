using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Data;
using PX.CCProcessingBase.Attributes;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.Descriptor;

namespace PX.Objects.CA
{
	#region CAOpenPeriodAttribute
	/// <summary>
	/// Specialized for CA version of the <see cref="OpenPeriodAttribut"/><br/>
	/// Selector. Provides  a list  of the active Fin. Periods, having CAClosed flag = false <br/>
	/// <example>
	/// [CAOpenPeriod(typeof(CATran.tranDate))]
	/// </example>
	/// </summary>
	public class CAOpenPeriodAttribute : OpenPeriodAttribute
	{
		#region Ctor

		/// <summary>
		/// Extended Ctor. 
		/// </summary>
		/// <param name="sourceType">Must be IBqlField. Refers a date, based on which "current" period will be defined</param>
		public CAOpenPeriodAttribute(Type sourceType, 
			Type branchSourceType,
			Type branchSourceFormulaType = null,
			Type organizationSourceType = null,
			Type useMasterCalendarSourceType = null,
			Type defaultType = null,
			bool redefaultOrRevalidateOnOrganizationSourceUpdated = true,
			Type masterFinPeriodIDType = null)
			: base(typeof(Search<FinPeriod.finPeriodID,
								Where<FinPeriod.cAClosed, Equal<False>,
										And<FinPeriod.status, Equal<FinPeriod.status.open>>>>), 
					sourceType,
					branchSourceType: branchSourceType,
					branchSourceFormulaType: branchSourceFormulaType,
					organizationSourceType: organizationSourceType,
					useMasterCalendarSourceType: useMasterCalendarSourceType,
					defaultType: defaultType,
					redefaultOrRevalidateOnOrganizationSourceUpdated: redefaultOrRevalidateOnOrganizationSourceUpdated,
					masterFinPeriodIDType: masterFinPeriodIDType)
		{
		}

		public CAOpenPeriodAttribute()
			: this(null, null)
		{
		}
		#endregion

		#region Implementation

		protected override PeriodValidationResult ValidateOrganizationFinPeriodStatus(PXCache sender, object row, FinPeriod finPeriod)
		{
			PeriodValidationResult result = base.ValidateOrganizationFinPeriodStatus(sender, row, finPeriod);

			if (!result.HasWarningOrError && finPeriod.CAClosed == true)
			{
				result = HandleErrorThatPeriodIsClosed(sender, finPeriod, errorMessage: Messages.FinancialPeriodClosedInCA);
			}

			return result;
		}

		#endregion
	}
	#endregion
}
