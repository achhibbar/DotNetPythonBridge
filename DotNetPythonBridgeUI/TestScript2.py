import sys

def main(args):
    if len(args) < 2:
        print("Usage: python TestScript2.py <arg1> <arg2>")
        return

    arg1 = args[0]
    arg2 = args[1]

    print(f"Argument 1: {arg1}")
    print(f"Argument 2: {arg2}")

if __name__ == "__main__":
    main(sys.argv[1:])
