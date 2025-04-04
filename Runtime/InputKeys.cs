using Moths.Inputs.Attributes;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moths.Inputs
{
	public struct InputKey
	{
		private Guid _guid;
		private string _id;
		private string _name;

		public string Name => _name;

		public Guid Guid => _guid;

		public static implicit operator InputKey(string name_id)
		{
			string[] split = name_id.Split("_");
			return new InputKey
			{
				_name = split[0],
				_id = split[1],
				_guid = new Guid(split[1]),
			};
		}

		public static implicit operator string(InputKey input) => input._id;

		public static implicit operator InputKey(InputActionReference action) => new() { _guid = action.action.id, _id = "", _name = action.name };

		public static bool operator ==(InputKey input1, InputKey input2) => input1._guid.Equals(input2._guid);
		public static bool operator !=(InputKey input1, InputKey input2) => !input1._guid.Equals(input2._guid);
		public static bool operator ==(InputKey input, InputAction action) => input._guid.Equals(action.id);
		public static bool operator !=(InputKey input, InputAction action) => !input._guid.Equals(action.id);
		public static bool operator ==(InputKey input, InputActionReference action) => input._guid.Equals(action.action.id);
		public static bool operator !=(InputKey input, InputActionReference action) => !input._guid.Equals(action.action.id);

		public override bool Equals(object obj)
		{
			return obj is InputKey input &&
					_guid.Equals(input._guid);
		}
		public bool Equals(InputKey input)
		{
			return this == input;
		}

		public override int GetHashCode()
		{
			return _guid.GetHashCode();
		}
	}
}