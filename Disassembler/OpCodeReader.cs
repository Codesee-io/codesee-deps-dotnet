
using System.Reflection;
using System.Reflection.Emit;

namespace Disassembler
{
    internal class OpCodeReader
    {
        static readonly OpCode[] singleByteOpCodes;
        static readonly OpCode[] twoByteOpCodes;
        /**
         * The OpCodes class in System.Reflection.Emit has fields for all of the
         * OpCodes.  Statically initialize a list of single byte and 2 byte
         * codes for later lookup.
         */
        static OpCodeReader() { 
            singleByteOpCodes= new OpCode[256];
            twoByteOpCodes= new OpCode[256];
            var allCodes = typeof(OpCodes).GetFields();
            foreach(var code in allCodes)
            {
                OpCode? opCode = (OpCode?)code.GetValue(null);
                if (opCode != null)
                {
                    ushort instruction = (ushort)((OpCode)opCode).Value;
                    //Single byte
                    if (instruction < 256)
                    {
                        singleByteOpCodes[instruction] = (OpCode)opCode;
                    }
                    else
                    {
                        //Two bytes
                        twoByteOpCodes[0xFF & instruction] = (OpCode)opCode;
                    }
                }
            }
        }

        private readonly byte[] ilCode;
        private int ilOffset = 0;
        private readonly Module module;
        private readonly ReferenceCollector collector;
        private readonly MethodBase method;

        //Filters so that we only process a token once
        private readonly HashSet<int> knownMethods = new();
        private readonly HashSet<int> knownTypes = new();
        private readonly HashSet<int> knownFields = new();

        /**
         * The OpCodeReader processes the ilCode for any references.  Those references are pushed in to the
         * ReferenceCollector while processing.  Call Read() to process the IL code.
         */
        public OpCodeReader(MethodBase method, byte[] ilCode, Module module, ReferenceCollector collector)
        {
            this.ilCode = ilCode;
            this.module = module;
            this.collector = collector;
            this.method = method;
        }

        /**
         * Reads the IL code provided in the constuctor.
         */
        public void Read()
        {
            ilOffset = 0;
            while(ilOffset < ilCode.Length)
            {
                OpCode op = NextOpCode();
                ProcessOperand(op);
            }
        }
        /**
         * Consume the next code.  It will advance one or two bytes of the
         * ilCode array depending on the instruction.
         */
        private OpCode NextOpCode()
        {
            var lowByte = ilCode[ilOffset++];
            if(lowByte != 0xfe)
            {
                return singleByteOpCodes[lowByte];
            }
            else
            {
                var highByte = ilCode[ilOffset++];
                return twoByteOpCodes[highByte];
            }
            
        }

        private int ReadInt32()
        {
            return ((ilCode[ilOffset++] | (ilCode[ilOffset++] << 8)) | (ilCode[ilOffset++] << 16)) | (ilCode[ilOffset++] << 24);
        }
    
        private void Skip16()
        {
            ilOffset += 2;
        }

        private void Skip32()
        {
            ilOffset += 4;
        }

        private void Skip64() 
        {
            ilOffset += 8;
        }

        private void Skip8()
        {
            ilOffset += 1;
        }

        /**
         * Processes a type token for references.
         */
        private void TypeFound(int typeToken)
        {
            if (!knownTypes.Add(typeToken))
            {
                return;
            }

            try
            {
                Type? resolved = null;
                if (method.DeclaringType != null)
                {
                    //We known constructors can't have generic arguments.
                    //A coll to GetGenericArguments throws on a constructor,
                    //so avoid the exception if we known it is a constructor
                    if (!method.IsConstructor)
                    {
                        try
                        {
                            resolved = module.ResolveType(typeToken, method.DeclaringType.GenericTypeArguments, method.GetGenericArguments());
                        }
                        catch (Exception)
                        {
                            //Some compiler generated methods throw when you call GetGenericArguments.
                            //If that happen, we don't have generic arguments, so resolve it with only the
                            //method's type's generic arguments
                            resolved = module.ResolveType(typeToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                        }
                    }
                    else
                    {
                        resolved = module.ResolveType(typeToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                    }
                }
                if (resolved != null)
                {
                    collector.ReferenceTypeAndArgs(resolved);
                }
            }
            catch (Exception ex)
            {
                //TODO: Log to output object
                Console.WriteLine(ex.ToString());
            }
        }

        private void FieldFound(int fieldToken)
        {
            if (!knownFields.Add(fieldToken))
            {
                return;
            }
            try
            {
                FieldInfo? field = null;
                if (method.DeclaringType != null)
                {
                    //We known constructors can't have generic arguments.
                    //A coll to GetGenericArguments throws on a constructor,
                    //so avoid the exception if we known it is a constructor
                    if (!method.IsConstructor)
                    {
                        try
                        {
                            field = module.ResolveField(fieldToken, method.DeclaringType.GetGenericArguments(), method.GetGenericArguments());
                        }
                        catch (Exception)
                        {
                            //Some compiler generated methods throw when you call GetGenericArguments.
                            //If that happen, we don't have generic arguments, so resolve it with only the
                            //method's type's generic arguments
                            field = module.ResolveField(fieldToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                        }
                    }
                    else
                    {
                        field = module.ResolveField(fieldToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                    }
                }
                if (field != null)
                {
                    collector.ReferenceType(field.FieldType);
                }
            }
            catch (Exception ex)
            {
                //TODO: Log to output object
                Console.WriteLine(method.ToString());
                Console.WriteLine(ex.ToString());
            }
        }

        private void MethodFound(int methodToken)
        {
            if (!knownMethods.Add(methodToken))
            {
                return;
            }
            try
            {
                MethodBase? resolvedMethod = null;

                if (method.DeclaringType != null)
                {
                    //We known constructors can't have generic arguments.
                    //A coll to GetGenericArguments throws on a constructor,
                    //so avoid the exception if we known it is a constructor
                    if (!method.IsConstructor)
                    {
                        try
                        {
                            resolvedMethod = module.ResolveMethod(methodToken, method.DeclaringType.GetGenericArguments(), method.GetGenericArguments());
                        }catch(Exception) {
                            //Some compiler generated methods throw when you call GetGenericArguments.
                            //If that happen, we don't have generic arguments, so resolve it with only the
                            //method's type's generic arguments
                            resolvedMethod = module.ResolveMethod(methodToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                        }
                    }
                    else
                    {
                        resolvedMethod = module.ResolveMethod(methodToken, method.DeclaringType.GetGenericArguments(), Array.Empty<Type>());
                    }
                }
                if(resolvedMethod != null)
                {
                    foreach(var parameter in resolvedMethod.GetParameters())
                    {
                        collector.ReferenceTypeAndArgs(parameter.ParameterType);
                    }
                    if (resolvedMethod.DeclaringType != null)
                    {
                        collector.ReferenceTypeAndArgs(resolvedMethod.DeclaringType);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                //TODO: Log to output object
                Console.WriteLine(method.ToString());
                Console.WriteLine(ex.ToString());
            }
        }

        /**
         * Given an opcode, either consume it with a "Skip" method,
         * or read a reference token out and resolve it.
         */
        private void ProcessOperand(OpCode code)
        {
            switch (code.OperandType)
            {
                //Interesting Operands:
                case OperandType.InlineType:
                    var typeToken = ReadInt32();
                    TypeFound(typeToken);
                    break;
                case OperandType.InlineField:
                    var fieldToken = ReadInt32();
                    FieldFound(fieldToken);
                    break;
                case OperandType.InlineMethod:
                    var methodToken = ReadInt32();
                    MethodFound(methodToken);
                    break;
                //Uninteresting operands that we skip
                case OperandType.InlineBrTarget:
                    Skip32();
                    break;
                case OperandType.InlineI:
                    Skip32(); 
                    break;
                case OperandType.InlineI8:
                    Skip64();
                    break;
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineR:
                    Skip64();
                    break;
                case OperandType.InlineSig:
                    Skip32();
                    break;
                case OperandType.InlineString:
                    Skip32();
                    break;
                case OperandType.InlineSwitch:
                    //A switch instruction has a count, and then a series of 
                    //addresses that are the jump points.
                    //Read the count and then skip over the addresses.
                    int count = ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        Skip32();   
                    }
                    break;
                case OperandType.InlineTok:
                    Skip32();
                    break;
                case OperandType.InlineVar:
                    Skip16();
                    break;
                case OperandType.ShortInlineBrTarget:
                    Skip8();
                    break;
                case OperandType.ShortInlineI:
                    Skip8();
                    break;
                case OperandType.ShortInlineR:
                    Skip32();
                    break;
                case OperandType.ShortInlineVar:
                    Skip8();
                    break;
            }

        }
    }
}
