using PX.Data;

namespace PX.Objects.Common
{
	public static class Utilities
	{
		public static void Swap<T>(ref T first, ref T second)
		{
			T temp = first;
			first = second;
			second = temp;
		}

		public static string GetSubledgerTitle(this PXGraph graph, string subledgerPrefix)
		{
			using (new PXLoginScope(PXAccess.GetFullUserName(), PXAccess.GetAdministratorRoles()))
			{
				return ((PXDatabaseSiteMapProvider)PXSiteMap.Provider).FindSiteMapNodeByScreenID($"{subledgerPrefix ?? string.Empty}000000")?.Title;
			}
		}

		public static string GetSubledgerTitle<TSubledgerConst>(this PXGraph graph)
			where TSubledgerConst : IConstant<string>, IBqlOperand, new()
		{
			return graph.GetSubledgerTitle(new TSubledgerConst().Value.ToString());
		}

	}
}
