﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Backpropagation.ANN.Interfaces;
using Backpropagation.Structures;
using BackpropagationHandler = Backpropagation.Structures.Backpropagation;

namespace Backpropagation.ANN
{
	public class NeuralNetwork
	{
		public int NumberOfLayers { get; private set; }
		private List<int> _architecture;
		private IActivationFunction _function;
		private IActivationFunction _outputLayerFunction;
		private List<NeuronLayer> _layers;
		private BackpropagationType _bacpropagationType;
		private double _eta;
		private int _maxIteration;
		private double[] _perSymbolError;
		private Instance _instance;

		public const double EtaMin = 0.001;
		public const double EtaDefault = 0.01;
		public const double EtaMax = 1;

		public const int LimitMin = 100;
		public const int LimitDefault = 10000;
		public const int LimitMax = 1000000;

		public const int NeuronMin = 1;
		private readonly int _neuronDefault;
		public const int NeuronMax = 50;

		public NeuralNetwork(Instance instance, IActivationFunction function = null, IActivationFunction outputLayerFunction = null)
		{
			_instance = instance;
			int inputSize = instance.NumSymbolSamples * 2;
			int outputSize = instance.NumSymbols;
			_neuronDefault = outputSize;
			_eta = EtaDefault;
			_maxIteration = LimitDefault;
			List<int> architecture = new List<int> {inputSize, _neuronDefault, outputSize};
			InitNeuralNetwork(architecture, function, outputLayerFunction);
		}

		private void InitNeuralNetwork(List<int> architecture, IActivationFunction function = null, IActivationFunction outputLayerFunction = null)
		{
			if (architecture.Count < 3)
				throw new ArgumentException(@"Architecture must contain at least one hidden layer!", architecture.ToString());
			NumberOfLayers = architecture.Count;
			_architecture = architecture;
			_function = function;
			_outputLayerFunction = outputLayerFunction;
			_perSymbolError = new double[_instance.NumSymbols];
			
			for (int i = 0; i < _instance.NumSymbols; i++)
				_perSymbolError[i] = 0;
		}

		public void AddLayer()
		{
			NumberOfLayers++;
			_architecture.Insert(_architecture.Count - 1, _neuronDefault);
		}

		public void RemoveLayer()
		{
			NumberOfLayers--;
			_architecture.RemoveAt(_architecture.Count - 1);
		}

		public void UpdateLayer(int index, int value)
		{
			if (index == 0 || index == NumberOfLayers - 1)
				throw new Exception("Input and output layers cannot be changed!");
			_architecture[index] = value;
		}

		public void ChangeEta(double eta)
		{
			_eta = eta;
		}

		public double GetEta()
		{
			return _eta;
		}

		public void ChangeIterations(int iterations)
		{
			_maxIteration = iterations;
		}

		public int GetIterations()
		{
			return _maxIteration;
		}

		private void InitNetwork()
		{
			_layers = new List<NeuronLayer>();
			
			for (int i = 0; i < NumberOfLayers; i++)
			{
				int numberOfNeuronsInFormerLayer = i == 0 ? 0 : _architecture[i - 1];
				IActivationFunction currentFunction = i == NumberOfLayers - 1 ? _outputLayerFunction : _function;
				_layers.Add(new NeuronLayer(_architecture[i], numberOfNeuronsInFormerLayer, currentFunction));
			}
			Evaluate();
		}

		public void Train()
		{
			InitNetwork();
		}

		public void ResetNetwork()
		{
			for (var i = 0; i < NumberOfLayers; i++)
			{
				_layers[i].ResetLayer();
			}
		}

		public double[] GetOutputs(double[] xValues, double[] yValues)
		{
			double[] input = FormatInput(xValues, yValues);
			return GetOutputs(input);
		}

		private double[] FormatInput(double[] xValues, double[] yValues)
		{
			return xValues.Concat(yValues).ToArray();
		}

		private double[] GetOutputs(double[] inputs)
		{
			double[] tempInputs = inputs;
			for (int i = 0; i < NumberOfLayers; i++)
			{
				tempInputs = _layers[i].GetOutputs(tempInputs);
			}
			return tempInputs;
		}

		public List<int> GetArchitecture()
		{
			return _architecture;
		}

		public void Evaluate()
		{
			double[][] givenOutputs = new double[_instance.Symbols.Length][];
			int[][] expectedOutputs = new int[_instance.Symbols.Length][];

			for (int i = 0; i < _instance.Symbols.Length; i++)
			{
				givenOutputs[i] = GetOutputs(_instance.Symbols[i].XPositions, _instance.Symbols[i].YPositions);
				expectedOutputs[i] = _instance.Symbols[i].Class;

			}
			_perSymbolError = CriterionFunction.EvaluatePerSymbol(givenOutputs, expectedOutputs);
		}

		public bool IsEquals(List<int> architecture, IActivationFunction function = null, IActivationFunction outputLayerFunction = null)
		{
			if (NumberOfLayers != architecture.Count)
				return false;

			if (_function != function)
				return false;

			if (_outputLayerFunction != outputLayerFunction)
				return false;

			for (int i = 0; i < NumberOfLayers; i++)
			{
				if (_architecture[i] != architecture[i])
					return false;
			}
			return true;
		}

		public void OnValueChanged_Type(object sender, EventArgs e)
		{
			if (sender is ComboBox box)
			{
				_bacpropagationType = BackpropagationHandler.ToEnum((string)box.SelectedItem);
			}
		}

		public static void FillTrainChoices(Chart graph, TableLayoutPanel layoutArchitecture, ComboBox backpropagationType, TextBox eta, TextBox limit, NeuralNetwork ann)
		{
			FillChart(graph, ann);
			FillPanel(layoutArchitecture, ann);
			backpropagationType.Items.Add(BackpropagationHandler.ToString(BackpropagationType.Batch));
			backpropagationType.Items.Add(BackpropagationHandler.ToString(BackpropagationType.Online));
			backpropagationType.Items.Add(BackpropagationHandler.ToString(BackpropagationType.MiniBatch));
			backpropagationType.SelectedItem = backpropagationType.Items[0];
			eta.Text = EtaDefault.ToString(CultureInfo.InvariantCulture);
			limit.Text = LimitDefault.ToString(CultureInfo.InvariantCulture);
		}

		public static void FillChart(Chart graph, NeuralNetwork ann)
		{
			string name = "SymbolError";
			graph.Series[name].Points.Clear();
			graph.ChartAreas[0].AxisY.Maximum = 1;
			for (int i = 0; i < ann._instance.NumSymbols; i++)
			{
				graph.Series[name].Points.AddXY(i + 1, ann._perSymbolError[i]);
			}
		}

		public static void FillPanel(TableLayoutPanel panel, NeuralNetwork ann)
		{
			int rowCount = 1;
			int columnCount = ann.NumberOfLayers;
			panel.ColumnCount = columnCount;
			panel.RowCount = rowCount;

			panel.ColumnStyles.Clear();
			panel.RowStyles.Clear();
			panel.Controls.Clear();

			for (var i = 0; i < columnCount; i++)
			{
				panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
			}
			for (var i = 0; i < rowCount; i++)
			{
				panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
			}

			Label input = new Label
			{
				Text = ann._architecture[0].ToString(),
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Fill
			};
			panel.Controls.Add(input);

			
			for (int i = 1; i < ann.NumberOfLayers - 1; i++)
			{
				TextBox box = new TextBox
				{
					Text = ann._architecture[i].ToString(),
					Name = $"t_{i}",
					Dock = DockStyle.Fill
				};
				panel.Controls.Add(box);

			}
			
			Label output = new Label
			{
				Text = ann._architecture[ann.NumberOfLayers - 1].ToString(),
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Fill
			};
			panel.Controls.Add(output);
		}

		public static int WhichClass(double[] solution)
		{
			int whichClass = 0;
			double max = 0;
			for (int i = 0; i < solution.Length; i++)
			{
				if (!(solution[i] > max)) continue;
				max = solution[i];
				whichClass = i;
			}
			return whichClass;
		}
	}
}