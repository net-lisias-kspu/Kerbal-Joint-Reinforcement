using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

#if IncludeHook

using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace KerbalJointReinforcement
{
/*
	public enum ExceptionBlockType
	{
		/// <summary>The beginning of an exception block</summary>
		BeginExceptionBlock,
		/// <summary>The beginning of a catch block</summary>
		BeginCatchBlock,
		/// <summary>The beginning of an except filter block</summary>
		BeginExceptFilterBlock,
		/// <summary>The beginning of a fault block</summary>
		BeginFaultBlock,
		/// <summary>The beginning of a finally block</summary>
		BeginFinallyBlock,
		/// <summary>The end of an exception block</summary>
		EndExceptionBlock
	}

	/// <summary>An exception block</summary>
	public class ExceptionBlock
	{
		/// <summary>Block type</summary>
		public ExceptionBlockType blockType;

		/// <summary>Catch type</summary>
		public Type catchType;

		/// <summary>Creates an exception block</summary>
		/// <param name="blockType">Block type</param>
		/// <param name="catchType">Catch type</param>
		///
		public ExceptionBlock(ExceptionBlockType blockType, Type catchType = null)
		{
			this.blockType = blockType;
			this.catchType = catchType ?? typeof(object);
		}
	}

	/// <summary>An abstract wrapper around OpCode and their operands. Used by transpilers</summary>
	public class CodeInstruction
	{
		/// <summary>The opcode</summary>
		public OpCode opcode;
		/// <summary>The operand</summary>
		public object operand;
		/// <summary>All labels defined on this instruction</summary>
		public List<Label> labels = new List<Label>();
		/// <summary>All exception block boundaries defined on this instruction</summary>
		public List<ExceptionBlock> blocks = new List<ExceptionBlock>();

		/// <summary>Creates a new CodeInstruction with a given opcode and optional operand</summary>
		/// <param name="opcode">The code</param>
		/// <param name="operand">The operand</param>
		///
		public CodeInstruction(OpCode opcode, object operand = null)
		{
			this.opcode = opcode;
			this.operand = operand;
		}

		/// <summary>Create a full copy (including labels and exception blocks) of a CodeInstruction</summary>
		/// <param name="instruction">The instruction to copy</param>
		///
		public CodeInstruction(CodeInstruction instruction)
		{
			opcode = instruction.opcode;
			operand = instruction.operand;
			labels = instruction.labels.ToArray().ToList();
			blocks = instruction.blocks.ToArray().ToList();
		}

		/// <summary>Clones a CodeInstruction and resets its labels and exception blocks</summary>
		/// <returns>A lightweight copy of this code instruction</returns>
		///
		public CodeInstruction Clone()
		{
			return new CodeInstruction(this)
			{
				labels = new List<Label>(),
				blocks = new List<ExceptionBlock>()
			};
		}

		/// <summary>Clones a CodeInstruction, resets labels and exception blocks and sets its opcode</summary>
		/// <param name="opcode">The opcode</param>
		/// <returns>A copy of this CodeInstruction with a new opcode</returns>
		///
		public CodeInstruction Clone(OpCode opcode)
		{
			var instruction = Clone();
			instruction.opcode = opcode;
			return instruction;
		}

		/// <summary>Clones a CodeInstruction, resets labels and exception blocks and sets its operand</summary>
		/// <param name="operand">The opcode</param>
		/// <returns>A copy of this CodeInstruction with a new operand</returns>
		///
		public CodeInstruction Clone(object operand)
		{
			var instruction = Clone();
			instruction.operand = operand;
			return instruction;
		}

		/// <summary>Returns a string representation of the code instruction</summary>
		/// <returns>A string representation of the code instruction</returns>
		///
		public override string ToString()
		{
			var list = new List<string>();
			foreach (var label in labels)
				list.Add("Label" + label.GetHashCode());
			foreach (var block in blocks)
				list.Add("EX_" + block.blockType.ToString().Replace("Block", ""));

			var extras = list.Count > 0 ? " [" + string.Join(", ", list.ToArray()) + "]" : "";
			var operandStr = Emitter.FormatArgument(operand);
			if (operandStr != "") operandStr = " " + operandStr;
			return opcode + operandStr + extras;
		}
	}

	internal class ILInstruction
	{
		internal int offset;
		internal OpCode opcode;
		internal object operand;
		internal object argument;

		internal List<Label> labels = new List<Label>();
		internal List<ExceptionBlock> blocks = new List<ExceptionBlock>();

		internal ILInstruction(OpCode opcode, object operand = null)
		{
			this.opcode = opcode;
			this.operand = operand;
			argument = operand;
		}

		internal CodeInstruction GetCodeInstruction()
		{
			var instr = new CodeInstruction(opcode, argument);
			if (opcode.OperandType == OperandType.InlineNone)
				instr.operand = null;
			instr.labels = labels;
			instr.blocks = blocks;
			return instr;
		}

		internal int GetSize()
		{
			var size = opcode.Size;

			switch (opcode.OperandType)
			{
				case OperandType.InlineSwitch:
					size += (1 + ((Array)operand).Length) * 4;
					break;

				case OperandType.InlineI8:
				case OperandType.InlineR:
					size += 8;
					break;

				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineSig:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					size += 4;
					break;

				case OperandType.InlineVar:
					size += 2;
					break;

				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					size += 1;
					break;
			}

			return size;
		}

		public override string ToString()
		{
			var instruction = "";

			AppendLabel(ref instruction, this);
			instruction = instruction + ": " + opcode.Name;

			if (operand == null)
				return instruction;

			instruction += " ";

			switch (opcode.OperandType)
			{
				case OperandType.ShortInlineBrTarget:
				case OperandType.InlineBrTarget:
					AppendLabel(ref instruction, operand);
					break;

				case OperandType.InlineSwitch:
					var switchLabels = (ILInstruction[])operand;
					for (var i = 0; i < switchLabels.Length; i++)
					{
						if (i > 0)
							instruction += ",";

						AppendLabel(ref instruction, switchLabels[i]);
					}
					break;

				case OperandType.InlineString:
					instruction = instruction + "\"" + operand + "\"";
					break;

				default:
					instruction += operand;
					break;
			}

			return instruction;
		}

		static void AppendLabel(ref string str, object argument)
		{
			var instruction = argument as ILInstruction;
			if (instruction != null)
				str = str + "IL_" + instruction.offset.ToString("X4");
			else
				str = str + "IL_" + argument;
		}
	}

	internal class MethodCopier
	{
		readonly MethodBodyReader reader;
		readonly List<MethodInfo> transpilers = new List<MethodInfo>();

		internal MethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null)
		{
			reader = new MethodBodyReader(fromMethod, toILGenerator);
			reader.DeclareVariables(existingVariables);
			reader.ReadInstructions();
		}

		internal void Finalize(List<Label> endLabels)
		{
			reader.FinalizeILCodes(transpilers, endLabels);
		}
	}

	internal class MethodBodyReader
	{
		readonly ILGenerator generator;

		readonly MethodBase method;
		readonly Module module;
		readonly Type[] typeArguments;
		readonly Type[] methodArguments;
		readonly ByteBuffer ilBytes;
		readonly ParameterInfo this_parameter;
		readonly ParameterInfo[] parameters;
		readonly IList<ExceptionHandlingClause> exceptions;
		readonly List<ILInstruction> ilInstructions;
		readonly List<LocalVariableInfo> localVariables;

		LocalBuilder[] variables;

		internal static List<ILInstruction> GetInstructions(ILGenerator generator, MethodBase method)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));
			var reader = new MethodBodyReader(method, generator);
			reader.DeclareVariables(null);
			reader.ReadInstructions();
			return reader.ilInstructions;
		}

		internal MethodBodyReader(MethodBase method, ILGenerator generator)
		{
			this.generator = generator;
			this.method = method;
			module = method.Module;

			var body = method.GetMethodBody();
			if (body == null)
				throw new ArgumentException("Method " + method.FullDescription() + " has no body");

			var bytes = body.GetILAsByteArray();
			if (bytes == null)
				throw new ArgumentException("Can not get IL bytes of method " + method.FullDescription());
			ilBytes = new ByteBuffer(bytes);
			ilInstructions = new List<ILInstruction>((bytes.Length + 1) / 2);

			var type = method.DeclaringType;

			if (type.IsGenericType)
			{
				try { typeArguments = type.GetGenericArguments(); }
				catch { typeArguments = null; }
			}

			if (method.IsGenericMethod)
			{
				try { methodArguments = method.GetGenericArguments(); }
				catch { methodArguments = null; }
			}

			if (!method.IsStatic)
				this_parameter = new ThisParameter(method);
			parameters = method.GetParameters();

			if(body.LocalVariables != null)
				localVariables = body.LocalVariables.ToList();
			else
				localVariables = new List<LocalVariableInfo>();
			exceptions = body.ExceptionHandlingClauses;
		}

		internal void ReadInstructions()
		{
			while (ilBytes.position < ilBytes.buffer.Length)
			{
				var loc = ilBytes.position; // get location first (ReadOpCode will advance it)
				var instruction = new ILInstruction(ReadOpCode()) { offset = loc };
				ReadOperand(instruction);
				ilInstructions.Add(instruction);
			}

			ResolveBranches();
			ParseExceptions();
		}

		internal void DeclareVariables(LocalBuilder[] existingVariables)
		{
			if (generator == null) return;
			if (existingVariables != null)
				variables = existingVariables;
			else
				variables = localVariables.Select(lvi => generator.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray();
		}

		// process all jumps
		//
		void ResolveBranches()
		{
			foreach (var ilInstruction in ilInstructions)
			{
				switch (ilInstruction.opcode.OperandType)
				{
					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineBrTarget:
						ilInstruction.operand = GetInstruction((int)ilInstruction.operand, false);
						break;

					case OperandType.InlineSwitch:
						var offsets = (int[])ilInstruction.operand;
						var branches = new ILInstruction[offsets.Length];
						for (var j = 0; j < offsets.Length; j++)
							branches[j] = GetInstruction(offsets[j], false);

						ilInstruction.operand = branches;
						break;
				}
			}
		}

		// process all exception blocks
		//
		void ParseExceptions()
		{
			foreach (var exception in exceptions)
			{
				var try_start = exception.TryOffset;
				var try_end = exception.TryOffset + exception.TryLength - 1;

				var handler_start = exception.HandlerOffset;
				var handler_end = exception.HandlerOffset + exception.HandlerLength - 1;

				var instr1 = GetInstruction(try_start, false);
				instr1.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null));

				var instr2 = GetInstruction(handler_end, true);
				instr2.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null));

				// The FilterOffset property is meaningful only for Filter clauses. 
				// The CatchType property is not meaningful for Filter or Finally clauses. 
				//
				switch (exception.Flags)
				{
					case ExceptionHandlingClauseOptions.Filter:
						var instr3 = GetInstruction(exception.FilterOffset, false);
						instr3.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptFilterBlock, null));
						break;

					case ExceptionHandlingClauseOptions.Finally:
						var instr4 = GetInstruction(handler_start, false);
						instr4.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
						break;

					case ExceptionHandlingClauseOptions.Clause:
						var instr5 = GetInstruction(handler_start, false);
						instr5.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, exception.CatchType));
						break;

					case ExceptionHandlingClauseOptions.Fault:
						var instr6 = GetInstruction(handler_start, false);
						instr6.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock, null));
						break;
				}
			}
		}

		// used in FinalizeILCodes to convert short jumps to long ones
/*		static readonly Dictionary<OpCode, OpCode> shortJumps = new Dictionary<OpCode, OpCode>
		{
			{ OpCodes.Leave_S, OpCodes.Leave },
			{ OpCodes.Brfalse_S, OpCodes.Brfalse },
			{ OpCodes.Brtrue_S, OpCodes.Brtrue },
			{ OpCodes.Beq_S, OpCodes.Beq },
			{ OpCodes.Bge_S, OpCodes.Bge },
			{ OpCodes.Bgt_S, OpCodes.Bgt },
			{ OpCodes.Ble_S, OpCodes.Ble },
			{ OpCodes.Blt_S, OpCodes.Blt },
			{ OpCodes.Bne_Un_S, OpCodes.Bne_Un },
			{ OpCodes.Bge_Un_S, OpCodes.Bge_Un },
			{ OpCodes.Bgt_Un_S, OpCodes.Bgt_Un },
			{ OpCodes.Ble_Un_S, OpCodes.Ble_Un },
			{ OpCodes.Br_S, OpCodes.Br },
			{ OpCodes.Blt_Un_S, OpCodes.Blt_Un }
		};

		internal void FinalizeILCodes(List<MethodInfo> transpilers, List<Label> endLabels)
		{
			if (generator == null) return;

			// pass1 - define labels and add them to instructions that are target of a jump
			//
			foreach (var ilInstruction in ilInstructions)
			{
				switch (ilInstruction.opcode.OperandType)
				{
					case OperandType.InlineSwitch:
						{
							var targets = ilInstruction.operand as ILInstruction[];
							if (targets != null)
							{
								var labels = new List<Label>();
								foreach (var target in targets)
								{
									var label = generator.DefineLabel();
									target.labels.Add(label);
									labels.Add(label);
								}
								ilInstruction.argument = labels.ToArray();
							}
							break;
						}

					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineBrTarget:
						{
							var target = ilInstruction.operand as ILInstruction;
							if (target != null)
							{
								var label = generator.DefineLabel();
								target.labels.Add(label);
								ilInstruction.argument = label;
							}
							break;
						}
				}
			}

			// pass2 - filter through all processors
			//
			var codeTranspiler = new CodeTranspiler(ilInstructions);
			transpilers.Do(transpiler => codeTranspiler.Add(transpiler));
			var codeInstructions = codeTranspiler.GetResult(generator, method);

			if (Harmony.DEBUG)
				Emitter.LogComment(generator, "start original");

			// pass3 - log out all new local variables
			//
			var savedLog = FileLog.GetBuffer(true);
			Emitter.AllLocalVariables(generator).Do(local => Emitter.LogLocalVariable(local));
			FileLog.LogBuffered(savedLog);

			// pass4 - remove RET if it appears at the end
			//
			while (true)
			{
				var lastInstruction = codeInstructions.LastOrDefault();
				if (lastInstruction == null || lastInstruction.opcode != OpCodes.Ret) break;

				// remember any existing labels
				endLabels.AddRange(lastInstruction.labels);

				codeInstructions.RemoveAt(codeInstructions.Count - 1);
			}

			// pass5 - mark labels and exceptions and emit codes
			//
			var idx = 0;
			codeInstructions.Do(codeInstruction =>
			{
				// mark all labels
				codeInstruction.labels.Do(label => Emitter.MarkLabel(generator, label));

				// start all exception blocks
				// TODO: we ignore the resulting label because we have no way to use it
				//
				codeInstruction.blocks.Do(block =>
				{
					Emitter.MarkBlockBefore(generator, block, out var label);
				});

				var code = codeInstruction.opcode;
				var operand = codeInstruction.operand;

				// replace RET with a jump to the end (outside this code)
				if (code == OpCodes.Ret)
				{
					var endLabel = generator.DefineLabel();
					code = OpCodes.Br;
					operand = endLabel;
					endLabels.Add(endLabel);
				}

				// replace short jumps with long ones (can be optimized but requires byte counting, not instruction counting)
				OpCode longJump;
				if (shortJumps.TryGetValue(code, out longJump))
					code = longJump;

				var emitCode = true;

				//if (code == OpCodes.Leave || code == OpCodes.Leave_S)
				//{
				//	// skip LEAVE on EndExceptionBlock
				//	if (codeInstruction.blocks.Any(block => block.blockType == ExceptionBlockType.EndExceptionBlock))
				//		emitCode = false;

				//	// skip LEAVE on next instruction starts a new exception handler and we are already in 
				//	if (idx < instructions.Length - 1)
				//		if (instructions[idx + 1].blocks.Any(block => block.blockType != ExceptionBlockType.EndExceptionBlock))
				//			emitCode = false;
				//}

				if (emitCode)
				{
					switch (code.OperandType)
					{
						case OperandType.InlineNone:
							Emitter.Emit(generator, code);
							break;

						case OperandType.InlineSig:

							// TODO the following will fail because we do not convert the token (operand)
							// All the decompilers can show the arguments correctly, we just need to find out how
							//
							if (operand == null) throw new Exception("Wrong null argument: " + codeInstruction);
							if ((operand is int) == false) throw new Exception("Wrong Emit argument type " + operand.GetType() + " in " + codeInstruction);
							Emitter.Emit(generator, code, (int)operand);

							/*
							// the following will only work if we can convert the original signature token to the required arguments
							//
							var callingConvention = System.Runtime.InteropServices.CallingConvention.ThisCall;
							var returnType = typeof(object);
							var parameterTypes = new[] { typeof(object) };
							Emitter.EmitCalli(generator, code, callingConvention, returnType, parameterTypes);

							var callingConventions = System.Reflection.CallingConventions.Standard;
							var optionalParameterTypes = new[] { typeof(object) };
							Emitter.EmitCalli(generator, code, callingConventions, returnType, parameterTypes, optionalParameterTypes);
							*/
/*							break;

						default:
							if (operand == null) throw new Exception("Wrong null argument: " + codeInstruction);
							var emitMethod = EmitMethodForType(operand.GetType());
							if (emitMethod == null) throw new Exception("Unknown Emit argument type " + operand.GetType() + " in " + codeInstruction);
							if (Harmony.DEBUG) FileLog.LogBuffered(Emitter.CodePos(generator) + code + " " + Emitter.FormatArgument(operand));
							emitMethod.Invoke(generator, new object[] { code, operand });
							break;
					}
				}

				codeInstruction.blocks.Do(block => Emitter.MarkBlockAfter(generator, block));

				idx++;
			});

			if (Harmony.DEBUG)
				Emitter.LogComment(generator, "end original");
		}

		// interpret member info value
		//
		static void GetMemberInfoValue(MemberInfo info, out object result)
		{
			result = null;
			switch (info.MemberType)
			{
				case MemberTypes.Constructor:
					result = (ConstructorInfo)info;
					break;

				case MemberTypes.Event:
					result = (EventInfo)info;
					break;

				case MemberTypes.Field:
					result = (FieldInfo)info;
					break;

				case MemberTypes.Method:
					result = (MethodInfo)info;
					break;

				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					result = (Type)info;
					break;

				case MemberTypes.Property:
					result = (PropertyInfo)info;
					break;
			}
		}

		// interpret instruction operand
		//
		void ReadOperand(ILInstruction instruction)
		{
			switch (instruction.opcode.OperandType)
			{
				case OperandType.InlineNone:
					{
						instruction.argument = null;
						break;
					}

				case OperandType.InlineSwitch:
					{
						var length = ilBytes.ReadInt32();
						var base_offset = ilBytes.position + (4 * length);
						var branches = new int[length];
						for (var i = 0; i < length; i++)
							branches[i] = ilBytes.ReadInt32() + base_offset;
						instruction.operand = branches;
						break;
					}

				case OperandType.ShortInlineBrTarget:
					{
						var val = (sbyte)ilBytes.ReadByte();
						instruction.operand = val + ilBytes.position;
						break;
					}

				case OperandType.InlineBrTarget:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = val + ilBytes.position;
						break;
					}

				case OperandType.ShortInlineI:
					{
						if (instruction.opcode == OpCodes.Ldc_I4_S)
						{
							var sb = (sbyte)ilBytes.ReadByte();
							instruction.operand = sb;
							instruction.argument = (sbyte)instruction.operand;
						}
						else
						{
							var b = ilBytes.ReadByte();
							instruction.operand = b;
							instruction.argument = (byte)instruction.operand;
						}
						break;
					}

				case OperandType.InlineI:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = val;
						instruction.argument = (int)instruction.operand;
						break;
					}

				case OperandType.ShortInlineR:
					{
						var val = ilBytes.ReadSingle();
						instruction.operand = val;
						instruction.argument = (float)instruction.operand;
						break;
					}

				case OperandType.InlineR:
					{
						var val = ilBytes.ReadDouble();
						instruction.operand = val;
						instruction.argument = (double)instruction.operand;
						break;
					}

				case OperandType.InlineI8:
					{
						var val = ilBytes.ReadInt64();
						instruction.operand = val;
						instruction.argument = (long)instruction.operand;
						break;
					}

				case OperandType.InlineSig:
					{
						var val = ilBytes.ReadInt32();
						var bytes = module.ResolveSignature(val);
						instruction.operand = bytes;
						instruction.argument = bytes;
						Debugger.Log(0, "TEST", "METHOD " + method.FullDescription() + "\n");
						Debugger.Log(0, "TEST", "Signature = " + bytes.Select(b => string.Format("0x{0:x02}", b)).Aggregate((a, b) => a + " " + b) + "\n");
						Debugger.Break();
						break;
					}

				case OperandType.InlineString:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = module.ResolveString(val);
						instruction.argument = (string)instruction.operand;
						break;
					}

				case OperandType.InlineTok:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = module.ResolveMember(val, typeArguments, methodArguments);
						GetMemberInfoValue((MemberInfo)instruction.operand, out instruction.argument);
						break;
					}

				case OperandType.InlineType:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = module.ResolveType(val, typeArguments, methodArguments);
						instruction.argument = (Type)instruction.operand;
						break;
					}

				case OperandType.InlineMethod:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = module.ResolveMethod(val, typeArguments, methodArguments);
						if (instruction.operand is ConstructorInfo)
							instruction.argument = (ConstructorInfo)instruction.operand;
						else
							instruction.argument = (MethodInfo)instruction.operand;
						break;
					}

				case OperandType.InlineField:
					{
						var val = ilBytes.ReadInt32();
						instruction.operand = module.ResolveField(val, typeArguments, methodArguments);
						instruction.argument = (FieldInfo)instruction.operand;
						break;
					}

				case OperandType.ShortInlineVar:
					{
						var idx = ilBytes.ReadByte();
						if (TargetsLocalVariable(instruction.opcode))
						{
							var lvi = GetLocalVariable(idx);
							if (lvi == null)
								instruction.argument = idx;
							else
							{
								instruction.operand = lvi;
								instruction.argument = variables[lvi.LocalIndex];
							}
						}
						else
						{
							instruction.operand = GetParameter(idx);
							instruction.argument = idx;
						}
						break;
					}

				case OperandType.InlineVar:
					{
						var idx = ilBytes.ReadInt16();
						if (TargetsLocalVariable(instruction.opcode))
						{
							var lvi = GetLocalVariable(idx);
							if (lvi == null)
								instruction.argument = idx;
							else
							{
								instruction.operand = lvi;
								instruction.argument = variables[lvi.LocalIndex];
							}
						}
						else
						{
							instruction.operand = GetParameter(idx);
							instruction.argument = idx;
						}
						break;
					}

				default:
					throw new NotSupportedException();
			}
		}

		ILInstruction GetInstruction(int offset, bool isEndOfInstruction)
		{
			var lastInstructionIndex = ilInstructions.Count - 1;
			if (offset < 0 || offset > ilInstructions[lastInstructionIndex].offset)
				throw new Exception("Instruction offset " + offset + " is outside valid range 0 - " + ilInstructions[lastInstructionIndex].offset);

			var min = 0;
			var max = lastInstructionIndex;
			while (min <= max)
			{
				var mid = min + ((max - min) / 2);
				var instruction = ilInstructions[mid];

				if (isEndOfInstruction)
				{
					if (offset == instruction.offset + instruction.GetSize() - 1)
						return instruction;
				}
				else
				{
					if (offset == instruction.offset)
						return instruction;
				}

				if (offset < instruction.offset)
					max = mid - 1;
				else
					min = mid + 1;
			}

			throw new Exception("Cannot find instruction for " + offset.ToString("X4"));
		}

		static bool TargetsLocalVariable(OpCode opcode)
		{
			return opcode.Name.Contains("loc");
		}

		LocalVariableInfo GetLocalVariable(int index)
		{
			return localVariables?[index];
		}

		ParameterInfo GetParameter(int index)
		{
			if (index == 0)
				return this_parameter;

			return parameters[index - 1];
		}

		OpCode ReadOpCode()
		{
			var op = ilBytes.ReadByte();
			return op != 0xfe
				? one_byte_opcodes[op]
				: two_bytes_opcodes[ilBytes.ReadByte()];
		}

		MethodInfo EmitMethodForType(Type type)
		{
			foreach (var entry in emitMethods)
				if (entry.Key == type) return entry.Value;
			foreach (var entry in emitMethods)
				if (entry.Key.IsAssignableFrom(type)) return entry.Value;
			return null;
		}

		// static initializer to prep opcodes

		static readonly OpCode[] one_byte_opcodes;
		static readonly OpCode[] two_bytes_opcodes;

		static readonly Dictionary<Type, MethodInfo> emitMethods;

		[MethodImpl(MethodImplOptions.Synchronized)]
		static MethodBodyReader()
		{
			one_byte_opcodes = new OpCode[0xe1];
			two_bytes_opcodes = new OpCode[0x1f];

			var fields = typeof(OpCodes).GetFields(
				BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields)
			{
				var opcode = (OpCode)field.GetValue(null);
				if (opcode.OpCodeType == OpCodeType.Nternal)
					continue;

				if (opcode.Size == 1)
					one_byte_opcodes[opcode.Value] = opcode;
				else
					two_bytes_opcodes[opcode.Value & 0xff] = opcode;
			}

			emitMethods = new Dictionary<Type, MethodInfo>();
			typeof(ILGenerator).GetMethods().ToList()
				.Do(method =>
				{
					if (method.Name != "Emit") return;
					var pinfos = method.GetParameters();
					if (pinfos.Length != 2) return;
					var types = pinfos.Select(p => p.ParameterType).ToArray();
					if (types[0] != typeof(OpCode)) return;
					emitMethods[types[1]] = method;
				});
		}

		// a custom this parameter

		class ThisParameter : ParameterInfo
		{
			internal ThisParameter(MethodBase method)
			{
				MemberImpl = method;
				ClassImpl = method.DeclaringType;
				NameImpl = "this";
				PositionImpl = -1;
			}
		}
	}
*/
	class KJRHooker
	{
/* eine erste Idee... ok, nicht vollständig, aber ... ginge evtl. so ungefähr -> sieht logisch aus
 * kommt hierher https://gitlab.altralogica.it/caos/insider/blob/master/Hook.cs

		public object CallOriginal(params object[] args)
		{
			Uninstall();
			var ret = _targetDelegate.DynamicInvoke(args);
			Install();
			return ret;
		}

		public bool Install()
		{
			try
			{
				if(_mPlatformTarget == PlatformTarget.Microsoft)
				{
					Marshal.Copy(_newBytes, 0, _target, _newBytes.Length);
				}
				else
				{
					unsafe
					{
						var sitePtr = (byte*) _target.ToPointer();
						*sitePtr = 0x49; // mov r11, target
						*(sitePtr + 1) = 0xBB;
						*((ulong*) (sitePtr + 2)) = (ulong) _hook.ToInt64();
						*(sitePtr + 10) = 0x41; // jmp r11
						*(sitePtr + 11) = 0xFF;
						*(sitePtr + 12) = 0xE3;
					}
				}
				IsInstalled = true;
				return true;
			}
			catch (Exception)
			{
				IsInstalled = false;
				return false;
			}
		}

		public bool Uninstall()
		{
			try
			{
				Marshal.Copy(_originalBytes, 0, _target, _originalBytes.Length);
				IsInstalled = false;
				return true;
			}
			catch (Exception)
			{
				IsInstalled = true;
				return false;
			}
		}	

		// The pointer to the hook.
		private readonly IntPtr _hook;
		
		// The method where to redirect your target
		private readonly MethodInfo _mHook;
		
		// Enum for internal operation
		private readonly PlatformTarget _mPlatformTarget;
		
		// The method to hook
		private readonly MethodInfo _mTarget;
		
		// The new bytes to be written to the target.
		private readonly byte[] _newBytes;
		
		// The original  bytes read from the target.
		private readonly byte[] _originalBytes;
		
		// The pointer to the target.
		private readonly IntPtr _target;
		
		// The delegate method of the target.
		private readonly Delegate _targetDelegate;

		// Initializes a new instance of the <see cref="Hook" /> class.
		public Hook(Delegate target, Delegate hook)
		{
			_mPlatformTarget = PlatformTarget.Microsoft;
			_targetDelegate = target;
			_hook = hook.Method.MethodHandle.GetFunctionPointer();
			_target = target.Method.MethodHandle.GetFunctionPointer();
			byte[] hookPointerBytes;
			if (IntPtr.Size == 8)
			{
				_originalBytes = new byte[6];
				Marshal.Copy(_target, _originalBytes, 0, 6);
				var diff = _hook.ToInt64() - _target.ToInt64() - 5L;
				hookPointerBytes = BitConverter.GetBytes(Convert.ToInt32(diff));
				_newBytes = new byte[]
				{
					0xE9, hookPointerBytes[0], hookPointerBytes[1], hookPointerBytes[2], hookPointerBytes[3]
				};
			}
			else
			{
				_originalBytes = new byte[6];
				Marshal.Copy(_target, _originalBytes, 0, 6);
				hookPointerBytes = BitConverter.GetBytes(_hook.ToInt32());
				_newBytes = new byte[]
				{
					0x68, hookPointerBytes[0], hookPointerBytes[1], hookPointerBytes[2], hookPointerBytes[3], 0xC3
				};
			}
		}
		
		// Initializes a new instance of the <see cref="Hook" /> class.
		public Hook(MethodInfo target, MethodInfo hook, Delegate targetDelegate)
		{
			_mPlatformTarget = PlatformTarget.Mono;
			_targetDelegate = targetDelegate;
			_mTarget = target;
			_mHook = hook;
			_target = _mTarget.MethodHandle.GetFunctionPointer();
			_hook = _mHook.MethodHandle.GetFunctionPointer();
			_originalBytes = new byte[13];
			Marshal.Copy(_target, _originalBytes, 0, 13);
		}
 */

	internal class ByteBuffer
	{
		internal byte[] buffer;

		internal int position;

		internal ByteBuffer(byte[] buffer)
		{
			this.buffer = buffer;
		}

		internal byte CheckByte()
		{
			return buffer[position];
		}

		internal byte ReadByte()
		{
			CheckCanRead(1);
			return buffer[position++];
		}

		internal byte[] ReadBytes(int length)
		{
			CheckCanRead(length);
			var value = new byte[length];
			Buffer.BlockCopy(buffer, position, value, 0, length);
			position += length;
			return value;
		}

		internal short ReadInt16()
		{
			CheckCanRead(2);
			var value = (short)(buffer[position]
				| (buffer[position + 1] << 8));
			position += 2;
			return value;
		}

		internal int ReadInt32()
		{
			CheckCanRead(4);
			var value = buffer[position]
				| (buffer[position + 1] << 8)
				| (buffer[position + 2] << 16)
				| (buffer[position + 3] << 24);
			position += 4;
			return value;
		}

		internal long ReadInt64()
		{
			CheckCanRead(8);
			var low = (uint)(buffer[position]
				| (buffer[position + 1] << 8)
				| (buffer[position + 2] << 16)
				| (buffer[position + 3] << 24));

			var high = (uint)(buffer[position + 4]
				| (buffer[position + 5] << 8)
				| (buffer[position + 6] << 16)
				| (buffer[position + 7] << 24));

			var value = (((long)high) << 32) | low;
			position += 8;
			return value;
		}

		internal float ReadSingle()
		{
			if (!BitConverter.IsLittleEndian)
			{
				var bytes = ReadBytes(4);
				Array.Reverse(bytes);
				return BitConverter.ToSingle(bytes, 0);
			}

			CheckCanRead(4);
			var value = BitConverter.ToSingle(buffer, position);
			position += 4;
			return value;
		}

		internal double ReadDouble()
		{
			if (!BitConverter.IsLittleEndian)
			{
				var bytes = ReadBytes(8);
				Array.Reverse(bytes);
				return BitConverter.ToDouble(bytes, 0);
			}

			CheckCanRead(8);
			var value = BitConverter.ToDouble(buffer, position);
			position += 8;
			return value;
		}

		void CheckCanRead(int count)
		{
			if (position + count > buffer.Length)
				throw new ArgumentOutOfRangeException();
		}
	}

		// Hilfsklasse, weil man sonst nicht an die blöden Daten kommt um sie zu sezten
		class ThisParameter : ParameterInfo
		{
			internal ThisParameter(MethodBase method)
			{
				MemberImpl = method;
				ClassImpl = method.DeclaringType;
				NameImpl = "this";
				PositionImpl = -1;
			}
		}

	// FEHLER, supertemp... muss einfach mal irgendwie laufen... verdammt
		static Module module;
		static Type[] typeArguments;
		static Type[] methodArguments;
		static ParameterInfo this_parameter;
		static ParameterInfo[] parameters;
		static List<LocalVariableInfo> localVariables;
		static IList<ExceptionHandlingClause> exceptions;

// FEHLER, find ich nicht so super diese Klasse, aber egal mal
	public enum ExceptionBlockType
	{
		/// <summary>The beginning of an exception block</summary>
		BeginExceptionBlock,
		/// <summary>The beginning of a catch block</summary>
		BeginCatchBlock,
		/// <summary>The beginning of an except filter block</summary>
		BeginExceptFilterBlock,
		/// <summary>The beginning of a fault block</summary>
		BeginFaultBlock,
		/// <summary>The beginning of a finally block</summary>
		BeginFinallyBlock,
		/// <summary>The end of an exception block</summary>
		EndExceptionBlock
	}
	public class ExceptionBlock
	{
		/// <summary>Block type</summary>
		public ExceptionBlockType blockType;

		/// <summary>Catch type</summary>
		public Type catchType;

		/// <summary>Creates an exception block</summary>
		/// <param name="blockType">Block type</param>
		/// <param name="catchType">Catch type</param>
		///
		public ExceptionBlock(ExceptionBlockType blockType, Type catchType = null)
		{
			this.blockType = blockType;
			this.catchType = catchType ?? typeof(object);
		}
	}

		class Instr
		{
		public int offset;
		public OpCode opcode;
		public object operand;
		public object argument;


		public bool isLabel = false;
		public Label label;
			// labels? ... das kenn ich noch nicht
		internal List<ExceptionBlock> blocks = new List<ExceptionBlock>();


		internal int GetSize()
		{
			var size = opcode.Size;

			switch (opcode.OperandType)
			{
				case OperandType.InlineSwitch:
					size += (1 + ((Array)operand).Length) * 4;
					break;

				case OperandType.InlineI8:
				case OperandType.InlineR:
					size += 8;
					break;

				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineSig:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					size += 4;
					break;

				case OperandType.InlineVar:
					size += 2;
					break;

				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					size += 1;
					break;
			}

			return size;
		}


		};
		static List<Instr> instrs;

// FEHLER, ok, hab's geparst, aber... die Exceptions noch nicht und die Sprünge hab ich noch nicht zusammengesucht... oder sowas... na mal sehen wo's hin geht
		static void TryToParseThis(byte[] il)
		{
			instrs = new List<Instr>();

			OpCode[] one = new OpCode[0xe1];
			OpCode[] two = new OpCode[0x1f];

			foreach(var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var opcode = (OpCode)field.GetValue(null);
				if(opcode.OpCodeType == OpCodeType.Nternal)
					continue;

				if(opcode.Size == 1)
					one[opcode.Value] = opcode;
				else
					two[opcode.Value & 0xff] = opcode;
			}


			ByteBuffer bf = new ByteBuffer(il);
			bf.position = 0;

			while(bf.position < il.Length)
			{
				Instr instr = new Instr();

				instr.offset = bf.position; // von der Operation (wegen den Sprüngen nehme ich an)

				// read opcode

				if(bf.CheckByte() == 0xfe)
				{
					++bf.position;
					instr.opcode = two[bf.ReadByte()];
				}
				else
					instr.opcode = one[bf.ReadByte()];

				// operand

				switch(instr.opcode.OperandType)
				{
					//     Der Operand ist ein Verzweigungsziel in Form einer 32-Bit-Ganzzahl.
				case OperandType.InlineBrTarget:
					{
						int val = bf.ReadInt32();
						instr.operand = val + bf.position;
					}
					break;

					//     Der Operand ist ein 32-Bit-Metadatentoken.
				case OperandType.InlineField:
					{
						int val = bf.ReadInt32();
						instr.operand = module.ResolveField(val, typeArguments, methodArguments);
						instr.argument = (FieldInfo)instr.operand;
					}
					break;

					//     Der Operand ist eine 32-Bit-Ganzzahl.
				case OperandType.InlineI:
					{
						int val = bf.ReadInt32();
						instr.operand = val;
						instr.argument = val;
					}
					break;

					//     Der Operand ist eine 64-Bit-Ganzzahl.
				case OperandType.InlineI8:
					{
						long val = bf.ReadInt64();
						instr.operand = val;
						instr.argument = val;
					}
					break;

					//     Der Operand ist ein 32-Bit-Metadatentoken.
				case OperandType.InlineMethod:
					{
						int val = bf.ReadInt32();
						instr.operand = module.ResolveMethod(val, typeArguments, methodArguments);
						if(instr.operand is ConstructorInfo)
							instr.argument = (ConstructorInfo)instr.operand;
						else
							instr.argument = (MethodInfo)instr.operand;
					}
					break;

					//     Kein Operand.
				case OperandType.InlineNone:
					instr.operand = null; // nehme ich mal an
					instr.argument = null;
					break;

					//     Dieser Operand ist reserviert und sollte nicht verwendet werden.
					// [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
			//	case OperandType.InlinePhi:
			//		break;

					//     Der Operand ist eine 64-Bit-IEEE-Gleitkommazahl.
				case OperandType.InlineR:
					{
						double val = bf.ReadDouble();
						instr.operand = val;
						instr.argument = val;
					}
					break;

					//     Der Operand ist ein 32-Bit-Metadatensignaturtoken.
				case OperandType.InlineSig:
					{
						int val = bf.ReadInt32();
						byte[] bts = module.ResolveSignature(val);
						instr.operand = bts;
						instr.argument = bts;
					}
					break;

					//     Der Operand ist ein 32-Bit-Metadatenzeichenfolgetoken.
				case OperandType.InlineString:
					{
						int val = bf.ReadInt32();
						instr.operand = module.ResolveString(val);
						instr.argument = (string)instr.operand;
					}
					break;

					//     Der Operand ist das 32-Bit-Ganzzahlargument zu einer switch-Anweisung.
				case OperandType.InlineSwitch:
					{
						int length = bf.ReadInt32();
						int baseOffset = bf.position + (length * 4);
						int[] branches = new int[length];
						for(int i = 0; i < length; i++)
							branches[i] = bf.ReadInt32() + baseOffset;
						instr.operand = branches;
					}
					break;

					//     Der Operand ist ein FieldRef-Token, ein MethodRef-Token oder ein TypeRef-Token.
				case OperandType.InlineTok:
					{
						int val = bf.ReadInt32();
						instr.operand = module.ResolveMember(val, typeArguments, methodArguments);
						instr.argument = null;
						switch (((MemberInfo)instr.operand).MemberType)
						{
							case MemberTypes.Constructor: instr.argument = (ConstructorInfo)instr.operand; break;
							case MemberTypes.Event: instr.argument = (EventInfo)instr.operand; break;
							case MemberTypes.Field: instr.argument = (FieldInfo)instr.operand; break;
							case MemberTypes.Method: instr.argument = (MethodInfo)instr.operand; break;
							case MemberTypes.TypeInfo:
							case MemberTypes.NestedType: instr.argument = (Type)instr.operand; break;
							case MemberTypes.Property: instr.argument = (PropertyInfo)instr.operand; break;
						}
					}
					break;

					//     Der Operand ist ein 32-Bit-Metadatentoken.
				case OperandType.InlineType:
					{
						int val = bf.ReadInt32();
						instr.operand = module.ResolveType(val, typeArguments, methodArguments);
						instr.argument = (Type)instr.operand;
					}
					break;

					//     Der Operand ist eine 16-Bit-Ganzzahl mit der Ordnungszahl einer lokalen Variablen
					//     oder einem Argument.
				case OperandType.InlineVar:

					//     Der Operand ist eine 8-Bit-Ganzzahl mit der Ordnungszahl einer lokalen Variablen
					//     oder einem Argument.
				case OperandType.ShortInlineVar:
					{
						short val;
						
						if(instr.opcode.OperandType == OperandType.InlineVar)
							val = bf.ReadInt16();
						else
							val = bf.ReadByte();

						if(instr.opcode.Name.Contains("loc"))
						{
							LocalVariableInfo lvi;
							if(val < localVariables.Count)
							{
								instr.operand = localVariables[val];
								instr.argument = localVariables[localVariables[val].LocalIndex]; // FEHLER, ist der localIndex nicht == val???
							}
							else
							{
								instr.operand = null; // nehme ich mal an
								instr.argument = val; // wozu auch immer
							}
						}
						else
						{
							if(val == 0)
								instr.operand = this_parameter;
							else
								instr.operand = parameters[val - 1];
							instr.argument = val;
						}
					}
					break;

					//
					// Zusammenfassung:
					//     Der Operand ist ein Verzweigungsziel in Form einer 8-Bit-Ganzzahl.
				case OperandType.ShortInlineBrTarget:
					{
						sbyte val = (sbyte)bf.ReadByte();
						instr.operand = val + bf.position;
					}
					break;

					//     Der Operand ist eine 8-Bit-Ganzzahl.
				case OperandType.ShortInlineI:
					{
						if(instr.opcode == OpCodes.Ldc_I4_S)
						{
							sbyte val = (sbyte)bf.ReadByte();
							instr.operand = val;
							instr.argument = val;
						}
						else
						{
							byte val = bf.ReadByte();
							instr.operand = val;
							instr.argument = val;
						}
					}
					break;

					//     Der Operand ist eine 32-Bit-IEEE-Gleitkommazahl.
				case OperandType.ShortInlineR:
					{
						float val = bf.ReadSingle();
						instr.operand = val;
						instr.argument = val;
					}
					break;

				default:
					// FEHLER, wir sind im Arsch -> abbrechen oder sowas...
					break;
				}

				instrs.Add(instr);
			}

			// hab's geparst und weggeschmissen... :-) -> behalten und 'ne Fkt. bauen draus wär doch 'ne Idee, oder??
		}

		static Instr FindInstrByStart(int offset)
		{
			int i = 0;
			while(i < instrs.Count)
			{
				if(instrs[i].offset < offset)
					++i;
				else if(instrs[i].offset > offset)
					return null;
				else
					return instrs[i];
			}

			return null;
		}

		static Instr FindInstrByEnd(int offset)
		{
			int i = 0;
			while(i < instrs.Count)
			{
				if(instrs[i].offset + instrs[i].GetSize() - 1 < offset)
					++i;
				else if(instrs[i].offset + instrs[i].GetSize() - 1 > offset)
					return null;
				else
					return instrs[i];
			}

			return null;
		}

		static void ResolveBranches()
		{
			foreach(var instr in instrs)
			{
				switch(instr.opcode.OperandType)
				{
					case OperandType.ShortInlineBrTarget:
						instr.operand = FindInstrByStart((int)Convert.ToByte(instr.operand));
						((Instr)instr.operand).isLabel = true;
						break;

					case OperandType.InlineBrTarget:
						instr.operand = FindInstrByStart(Convert.ToInt32(instr.operand));
						((Instr)instr.operand).isLabel = true;
						break;

					case OperandType.InlineSwitch:
						var offsets = (int[])instr.operand;
						var branches = new Instr[offsets.Length];
						for (var j = 0; j < offsets.Length; j++)
						{
							branches[j] = FindInstrByStart(offsets[j]);
							branches[j].isLabel = true;
						}

						instr.operand = branches;
						break;
				}
			}
		}

		static void ParseExceptions()
		{
			foreach(var exception in exceptions)
			{
				var try_start = exception.TryOffset;
				var try_end = exception.TryOffset + exception.TryLength - 1;

				var handler_start = exception.HandlerOffset;
				var handler_end = exception.HandlerOffset + exception.HandlerLength - 1;

				var instr1 = FindInstrByStart(try_start);
				instr1.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null));

				var instr2 = FindInstrByEnd(handler_end);
				instr2.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null));

				// The FilterOffset property is meaningful only for Filter clauses. 
				// The CatchType property is not meaningful for Filter or Finally clauses. 
				//
				switch (exception.Flags)
				{
					case ExceptionHandlingClauseOptions.Filter:
						var instr3 = FindInstrByStart(exception.FilterOffset);
						instr3.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptFilterBlock, null));
						break;

					case ExceptionHandlingClauseOptions.Finally:
						var instr4 = FindInstrByStart(handler_start);
						instr4.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
						break;

					case ExceptionHandlingClauseOptions.Clause:
						var instr5 = FindInstrByStart(handler_start);
						instr5.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, exception.CatchType));
						break;

					case ExceptionHandlingClauseOptions.Fault:
						var instr6 = FindInstrByStart(handler_start);
						instr6.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock, null));
						break;
				}
			}
		}

		// jetzt die Harmony-Version, die angeblich (laut ihnen) im KSP läuft... also gut... mal sehen

static bool versuchs = true;

		public static void VersuchMalWas()
		{
if(!versuchs)
	return;

			MethodInfo mi = typeof(Vessel).GetMethod("CycleAllAutoStrut");
			MethodBody mb = mi.GetMethodBody();
			byte[] il = mb.GetILAsByteArray();


			module = mi.Module;

			var type = mi.DeclaringType;

			if (type.IsGenericType)
			{
				try { typeArguments = type.GetGenericArguments(); }
				catch { typeArguments = null; }
			}

			if (mi.IsGenericMethod)
			{
				try { methodArguments = mi.GetGenericArguments(); }
				catch { methodArguments = null; }
			}

			if(mi.IsStatic)
				this_parameter = null;
			else
				this_parameter = new ThisParameter(mi); //??

			parameters = mi.GetParameters();
			
			if(mb.LocalVariables != null)
				localVariables = new List<LocalVariableInfo>(mb.LocalVariables); // oder? oder wie oder was??
			else
				localVariables = new List<LocalVariableInfo>();

			exceptions = mb.ExceptionHandlingClauses;

TryToParseThis(il);

			ResolveBranches();
			ParseExceptions();

	// FEHLER, nachher die Sprünge auflösen... wenn man will (um es zu verschieben nehme ich an)
	// und die Exceptions auflösen, damit wir die auch noch reinbringen ... danach den Müll abspielen und sehen ob's geht

			int l = mi.GetParameters().Length;

			DynamicMethod dm =
			CreatePatchedMethod(mi); // mal sehen ob's danach eine neue gibt im "Vessel"

//byte[] il2 = dm.GetMethodBody().GetILAsByteArray();

			var ilgen = dm.GetILGenerator();
			var fiBytes = ilgen.GetType().GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic);
			var fiLength = ilgen.GetType().GetField("m_length", BindingFlags.Instance | BindingFlags.NonPublic);
			byte[] il2 = fiBytes.GetValue(ilgen) as byte[];
			int cnt = (int)fiLength.GetValue(ilgen);


			versuchs = false; // einmal reicht
		}

		static readonly object locker = new object(); // FEHLER, na ja... weiss ned ob nötig für uns :-)

		public void Patch(MethodBase original)
		{
			lock(locker)
			{
				if(original == null)
					throw new NullReferenceException("wie soll ich bitte eine NULL-Funktion ersetzen, hä?");

	//			UpdateWrapper(original); // FEHLER, ja, sinnlos jetzt wo's gekürzt ist :-) -> ich will ja nur eine leere Funktion einfügen... ganz am Ende... vorerst nix anderes... ok? gut, super... also, weiter...
					// aber vorerst will ich nur mal eine Kopie der Funktion bauen und die einschleusen... die täte dann... genau das gleiche... klaro? gut, also ... weiter...
			}
		}

		/// <summary>Creates new dynamic method with the latest patches and detours the original method</summary>
		/// <param name="original">The original method</param>
		/// <param name="patchInfo">Information describing the patches</param>
		/// <param name="instanceID">Harmony ID</param>
		/// <returns>The newly created dynamic method</returns>
		///
/*		static void UpdateWrapper(MethodBase original)
		{
			var replacement = MethodPatcher.CreatePatchedMethod(original, null, instanceID, sortedPrefixes, sortedPostfixes, sortedTranspilers, sortedFinalizers);
				// nope, noch einfacher

			if (replacement == null) throw new MissingMethodException("Cannot create dynamic replacement for " + original.FullDescription());

			var errorString = Memory.DetourMethod(original, replacement);
			if (errorString != null)
				throw new FormatException("Method " + original.FullDescription() + " cannot be patched. Reason: " + errorString);

			PatchTools.RememberObject(original, replacement); // no gc for new value + release old value to gc

			return replacement;
		}
*/
		/// <summary>Detours a method</summary>
		/// <param name="original">The original method</param>
		/// <param name="replacement">The replacement method</param>
		/// <returns>An error string</returns>
		///
/*		public static string DetourMethod(MethodBase original, MethodBase replacement)
		{
			var originalCodeStart = GetMethodStart(original, out var exception);
			if (originalCodeStart == 0)
				return exception.Message;
			var patchCodeStart = GetMethodStart(replacement, out exception);
			if (patchCodeStart == 0)
				return exception.Message;

			return WriteJump(originalCodeStart, patchCodeStart);
		}
*/
		/// <summary>Gets the start of a method in memory</summary>
		/// <param name="method">The method</param>
		/// <param name="exception">[out] Details of the exception</param>
		/// <returns>The method start address</returns>
		///
/*		public static long GetMethodStart(MethodBase method, out Exception exception)
		{
			// required in .NET Core so that the method is JITed and the method start does not change
			//
			var handle = GetRuntimeMethodHandle(method);
			try
			{
				RuntimeHelpers.PrepareMethod(handle);
			}
#pragma warning disable RECS0022
			catch
#pragma warning restore RECS0022
			{
			}

			try
			{
				exception = null;
				return handle.GetFunctionPointer().ToInt64();
			}
			catch (Exception ex)
			{
				exception = ex;
				return 0;
			}
		}
*/
		/// <summary>Writes a jump to memory</summary>
		/// <param name="memory">The memory address</param>
		/// <param name="destination">Jump destination</param>
		/// <returns>An error string</returns>
		///
/*		public static string WriteJump(long memory, long destination)
		{
			UnprotectMemoryPage(memory);

			if (IntPtr.Size == sizeof(long))
			{
				if (CompareBytes(memory, new byte[] { 0xe9 }))
				{
					var offset = ReadInt(memory + 1);
					memory += 5 + offset;
				}

				memory = WriteBytes(memory, new byte[] { 0x48, 0xB8 });
				memory = WriteLong(memory, destination);
				_ = WriteBytes(memory, new byte[] { 0xFF, 0xE0 });
			}
			else
			{
				memory = WriteByte(memory, 0x68);
				memory = WriteInt(memory, (int)destination);
				_ = WriteByte(memory, 0xc3);
			}
			return null;
		}
*/
//		unpatch? ist das nicth einfach ein... retour-kopieren von dem was drin war?? verstehe ich nicht ganz


		unsafe public static void MarkForNoInlining(MethodBase method)
		{
			if(Type.GetType("Mono.Runtime") != null) // müsste ja wohl sein hier...
			{
				var iflags = (ushort*)(method.MethodHandle.Value) + 1;
				*iflags |= (ushort)MethodImplOptions.NoInlining;
			}
		}

		internal static DynamicMethod CreateDynamicMethod(MethodInfo original)
		{
			var patchName = original.Name + "_duArsch";
			patchName = patchName.Replace("<>", ""); // FEHLER, wieso soll das drin sein??

			Type[] parameterTypes = new Type[1];
			parameterTypes[0] = original.DeclaringType; // ja, this... weil die Methode statisch wird... wieso auch immer... aber ist egal jetzt

			DynamicMethod method;
			try
			{
				method = new DynamicMethod(
					patchName,
					MethodAttributes.Public | MethodAttributes.Static,
					CallingConventions.Standard,
					original.ReturnType,
					parameterTypes,
					original.DeclaringType,
					true
				);
			}
			catch(Exception)
			{ return null; }

		//	var offset = 1;
		//	for (var i = 0; i < parameters.Length; i++)
		//		method.DefineParameter(i + offset, parameters[i].Attributes, parameters[i].Name);
			// tun wir also nicht für das this? ... gut, von mir aus... aber aufpassen du!

			return method;
		}

		internal static LocalBuilder[] DeclareLocalVariables(MethodBase original, ILGenerator generator)
		{
			var vars = original.GetMethodBody().LocalVariables; // ja, GetMethodBody könnte NULL sein... ok, abfangen den Scheiss...
			if (vars == null)
				return new LocalBuilder[0];
			List<LocalBuilder> lb = new List<LocalBuilder>();
			for(int i = 0; i < vars.Count; i++)
				lb.Add(generator.DeclareLocal(vars[i].LocalType, vars[i].IsPinned));
			return lb.ToArray();
		}

	// FEHLER, Rückgabe ist Müll, aber ich weiss im Moment nicht, wie sie sein sollte
	public delegate object FastInvokeHandler(object target, object[] parameters);

		public static DynamicMethod CreatePatchedMethod(MethodInfo original)
		{
			try
			{
				if(original == null)
					throw new ArgumentNullException("ja ja, kann nicht sein, also lassen wir's gut sein, ok???");

				MarkForNoInlining(original);

			//	if (Harmony.DEBUG)
			//	{
			//		FileLog.LogBuffered("### Patch " + original.DeclaringType + ", " + original);
			//		FileLog.FlushBuffer();
			//	}

		//		var idx = postfixes.Count();
		//	var firstArgIsReturnBuffer = false; // -> void -> false
		//	var returnType = original.ReturnType; // void, wissen wir schon

				var patch = CreateDynamicMethod(original);
				if(patch == null)
					return null;
	
				ILGenerator il = patch.GetILGenerator();


if(false) // FEHLER, das hier wäre 1:1 kopieren per Murks... aber, wir haben's neu ja geparst... ok, ohne branch-Auflösung, aber hey, egal jetzt
{
				var flags = BindingFlags.NonPublic /*| BindingFlags.Public*/ | BindingFlags.Instance;
				var ctor = typeof(System.Reflection.Emit.OpCode).GetConstructors(flags); // , null, new Type[0], null);
				List<object> param = new List<object>();
int p = (68 << 8) // data
	| (18 << 16) | (0 << 24); // push, pop
int q = 1; // size -> rest is not interesting for us
				param.Add((int)p); param.Add((int)q);
				OpCode inst = (OpCode)ctor[0].Invoke(param.ToArray());

	//			DynamicILInfo ili = patch.GetDynamicILInfo();
	//			ili.SetCode(original.GetMethodBody().GetILAsByteArray(), original.GetMethodBody().MaxStackSize);

				for(int i = 0; i < 131; i++)
					il.Emit(inst);
}

// neue Idee -> die instrs ausgeben -> wenn das 1:1 genau gleich rauskäme... und funktionieren würde... dann würde ich das obere kopieren machen... weil das viiiiiiel einfacher ist und für meine Zwecke voll reicht
	// das ist also ein unnötiges Spielchen was ich hier treibe

				for(int i = 0; i < instrs.Count; i++)
				{
					if(instrs[i].isLabel)
						instrs[i].label = il.DefineLabel();
				}

				for(int i = 0; i < instrs.Count; i++)
				{
// blöcke; -> also anfänge -> FEHLER, aktuell gibt's keine in meiner Funktion, daher lass ich das mal weg -> die Ende der Blöcke dann auch noch setzen -> und... ja, Sprünge, die ignorier ich auch mal

					if(instrs[i].isLabel)
						il.MarkLabel(instrs[i].label);

					switch(instrs[i].opcode.OperandType)
					{
						//     Der Operand ist ein Verzweigungsziel in Form einer 32-Bit-Ganzzahl.
					case OperandType.InlineBrTarget:
						{
							il.Emit(instrs[i].opcode, ((Instr)(instrs[i].operand)).label);
						}
						break;

						//     Der Operand ist ein 32-Bit-Metadatentoken.
					case OperandType.InlineField:
						{
							il.Emit(instrs[i].opcode, (FieldInfo)instrs[i].operand);
						}
						break;

						//     Der Operand ist eine 32-Bit-Ganzzahl.
					case OperandType.InlineI:
						{
							il.Emit(instrs[i].opcode, Convert.ToInt32(instrs[i].operand));
						}
						break;

						//     Der Operand ist eine 64-Bit-Ganzzahl.
					case OperandType.InlineI8:
						{
							il.Emit(instrs[i].opcode, Convert.ToInt64(instrs[i].operand));
						}
						break;

						//     Der Operand ist ein 32-Bit-Metadatentoken.
					case OperandType.InlineMethod:
						{
							if(instrs[i].operand is ConstructorInfo)
								il.Emit(instrs[i].opcode, (ConstructorInfo)instrs[i].operand);
							else
								il.Emit(instrs[i].opcode, (MethodInfo)instrs[i].operand);
						}
						break;

						//     Kein Operand.
					case OperandType.InlineNone:
						il.Emit(instrs[i].opcode);
						break;

						//     Dieser Operand ist reserviert und sollte nicht verwendet werden.
						// [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
				//	case OperandType.InlinePhi:
				//		break;

						//     Der Operand ist eine 64-Bit-IEEE-Gleitkommazahl.
					case OperandType.InlineR:
						{
							il.Emit(instrs[i].opcode, Convert.ToDouble(instrs[i].operand));
						}
						break;

						//     Der Operand ist ein 32-Bit-Metadatensignaturtoken.
					case OperandType.InlineSig:
						{
	/*						int val = bf.ReadInt32();
							byte[] bts = module.ResolveSignature(val);
							instr.operand = bts;
							instr.argument = bts;*/
						}
						break;

						//     Der Operand ist ein 32-Bit-Metadatenzeichenfolgetoken.
					case OperandType.InlineString:
						{
							il.Emit(instrs[i].opcode, (string)instrs[i].operand);
						}
						break;

						//     Der Operand ist das 32-Bit-Ganzzahlargument zu einer switch-Anweisung.
					case OperandType.InlineSwitch:
						{
							Instr[] ops = (Instr[])instrs[i].operand;

							Label[] labels = new Label[ops.Length];

							for(int q = 0; q < ops.Length; q++)
								labels[q] = ops[q].label;

							il.Emit(instrs[i].opcode, labels);
						}
						break;

						//     Der Operand ist ein FieldRef-Token, ein MethodRef-Token oder ein TypeRef-Token.
					case OperandType.InlineTok:
						{
							switch (((MemberInfo)instrs[i].operand).MemberType)
							{
								case MemberTypes.Constructor:	il.Emit(instrs[i].opcode, (ConstructorInfo)instrs[i].operand); break;
				//				case MemberTypes.Event:			il.Emit(instrs[i].opcode, (EventInfo)instrs[i].operand); break;			-> FEHLER, gibt's das überhaupt???
								case MemberTypes.Field:			il.Emit(instrs[i].opcode, (FieldInfo)instrs[i].operand); break;
								case MemberTypes.Method:		il.Emit(instrs[i].opcode, (MethodInfo)instrs[i].operand); break;
								case MemberTypes.TypeInfo:
								case MemberTypes.NestedType:	il.Emit(instrs[i].opcode, (Type)instrs[i].operand); break;
				//				case MemberTypes.Property:		il.Emit(instrs[i].opcode, (PropertyInfo)instrs[i].operand); break;		-> FEHLER, gibt's das überhaupt???
							}
						}
						break;

						//     Der Operand ist ein 32-Bit-Metadatentoken.
					case OperandType.InlineType:
						{
							il.Emit(instrs[i].opcode, (Type)instrs[i].operand);
						}
						break;

						//     Der Operand ist eine 16-Bit-Ganzzahl mit der Ordnungszahl einer lokalen Variablen
						//     oder einem Argument.
					case OperandType.InlineVar:
						{
							il.Emit(instrs[i].opcode, Convert.ToInt16(instrs[i].operand));
						}
						break;

						//     Der Operand ist eine 8-Bit-Ganzzahl mit der Ordnungszahl einer lokalen Variablen
						//     oder einem Argument.
					case OperandType.ShortInlineVar:
						{
							il.Emit(instrs[i].opcode, Convert.ToByte(instrs[i].operand));
						}
						break;

						//
						// Zusammenfassung:
						//     Der Operand ist ein Verzweigungsziel in Form einer 8-Bit-Ganzzahl.
					case OperandType.ShortInlineBrTarget:
						{
							il.Emit(instrs[i].opcode, ((Instr)(instrs[i].operand)).label);
						}
						break;

						//     Der Operand ist eine 8-Bit-Ganzzahl.
					case OperandType.ShortInlineI:
						{
							if(instrs[i].opcode == OpCodes.Ldc_I4_S)
								il.Emit(instrs[i].opcode, Convert.ToSByte(instrs[i].operand));
							else
								il.Emit(instrs[i].opcode, Convert.ToByte(instrs[i].operand));
						}
						break;

						//     Der Operand ist eine 32-Bit-IEEE-Gleitkommazahl.
					case OperandType.ShortInlineR:
						{
							il.Emit(instrs[i].opcode, Convert.ToSingle(instrs[i].operand));
						}
						break;

					default:
						// FEHLER, wir sind im Arsch -> abbrechen oder sowas...
						break;
					}
				}

		//		var originalVariables = DeclareLocalVariables(original, il);
		//		var privateVars = new Dictionary<string, LocalBuilder>();

		//		LocalBuilder resultVariable = null; -> die ist verdammt noch mal void... also wozu soll das gut sein??
		//		if (idx > 0)
		//		{
		//			resultVariable = DynamicTools.DeclareLocalVariable(il, returnType);
		//			privateVars[RESULT_VAR] = resultVariable;
		//		}

		/*		prefixes.Union(postfixes).Union(finalizers).ToList().ForEach(fix =>
				{
					if (fix.DeclaringType != null && privateVars.ContainsKey(fix.DeclaringType.FullName) == false)
					{
						fix.GetParameters()
						.Where(patchParam => patchParam.Name == STATE_VAR)
						.Do(patchParam =>
						{
							var privateStateVariable = DynamicTools.DeclareLocalVariable(il, patchParam.ParameterType);
							privateVars[fix.DeclaringType.FullName] = privateStateVariable;
						});
					}
				});*/ // FEHLER, später vielleicht, im Moment haben wir das nicht

		//		var skipOriginalLabel = il.DefineLabel();
//AddPrefix(); // einfacher... viel einfacher oder? ... oder muss ich trotzdem noch alle jump und so lesen und so weiter? ... ja möglich

/*				var copier = new MethodCopier(source ?? original, il, originalVariables);

Zeug kopieren ohne grossen Aufwand... echt jetzt... was ist das Problem eigentlich? die jumps?
		internal MethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null)
		{
			if (fromMethod == null) throw new ArgumentNullException(nameof(fromMethod));
			reader = new MethodBodyReader(fromMethod, toILGenerator);
			reader.DeclareVariables(existingVariables);
			reader.ReadInstructions();
		}

				var endLabels = new List<Label>();
				copier.Finalize(endLabels);

ah genau :-) das ist das Problem hier
				foreach (var label in endLabels)
					Emitter.MarkLabel(il, label);
				if (resultVariable != null)
					Emitter.Emit(il, OpCodes.Stloc, resultVariable);
				if (canHaveJump)
					Emitter.MarkLabel(il, skipOriginalLabel);

				AddPostfixes(il, original, postfixes, privateVars, false);

				if (resultVariable != null)
					Emitter.Emit(il, OpCodes.Ldloc, resultVariable);

				AddPostfixes(il, original, postfixes, privateVars, true);

				Emitter.Emit(il, OpCodes.Ret);

		//		if (Harmony.DEBUG)
		//		{
		//			FileLog.LogBuffered("DONE");
		//			FileLog.LogBuffered("");
		//			FileLog.FlushBuffer();
		//		}
*/

		//		DynamicTools.PrepareDynamicMethod(patch); -> für .NET komplizierter, für mono... nicht so ganz

				var nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
	//			var nonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

			// on mono, just call 'CreateDynMethod'
			//

			var m_CreateDynMethod = patch.GetType().GetMethod("CreateDynMethod", nonPublicInstance);
			if (m_CreateDynMethod != null)
			{
				m_CreateDynMethod.Invoke(patch, new object[0]);

	//			var h_CreateDynMethod = FastInvokeHandler(m_CreateDynMethod, m_CreateDynMethod.DeclaringType.Module);
	//			h_CreateDynMethod(m_CreateDynMethod, new object[0]);
			}

				return patch;
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log(ex.Message);

//				var exceptionString = "Exception from HarmonyInstance \"" + harmonyInstanceID + "\" patching " + original.FullDescription() + ": " + ex;
		//		if (Harmony.DEBUG)
		//		{
		//			var savedIndentLevel = FileLog.indentLevel;
		//			FileLog.indentLevel = 0;
		//			FileLog.Log(exceptionString);
		//			FileLog.indentLevel = savedIndentLevel;
		//		}

//				throw new Exception(exceptionString, ex);
			}
			finally
			{
		//		if (Harmony.DEBUG)
		//			FileLog.FlushBuffer();
			}

			return null;
		}

	}
}

#endif
