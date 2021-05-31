#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <stdlib.h>
#include <vector>
#include <queue>
#include <omp.h>
#include <iostream>
#include <array>
#include <functional>
#include <string>
#include <vector>

#include <chrono>

using namespace std;

#define NUM_THREADS omp_get_max_threads()


vector<vector<int>>graph;
queue <int> qq;
int* marked = NULL;
unsigned int i, threads_num;
int res, nodesNum, node1, node2, node, root;



void s_init() {
	int res, nodesNum, node1, node2;

	FILE* fin = fopen("graph_200000_nodes_0.in", "r");

	fscanf(fin, "%d", &nodesNum);

	graph.resize(nodesNum + 1);
	marked = new int[nodesNum + 1]();

	while (fscanf(fin, "%d%d", &node1, &node2) != EOF) {
		bool checkNode = node1 < 0 || node2 < 0 || node1 > nodesNum || node2 > nodesNum;


		graph[node1].push_back(node2);
		graph[node2].push_back(node1);
	}

	res = fclose(fin);
}

void display() {
	for (unsigned int i = 0; i < graph.size(); ++i) {
		if (graph[i].size() != 0) {
			cout << i << "->";
			for (unsigned int j = 0; j < graph[i].size(); ++j) {
				cout << graph[i][j] << " ";
			}
			cout << "\n";
		}
	}
}

void s_bfs(int startNode) {

	qq.push(startNode);

	while (!qq.empty()) {
		int node = qq.front();
		qq.pop();

		marked[node] = 2;
		//cout << node;

		for (unsigned int i = 0; i < graph[node].size(); ++i) {
			if (marked[graph[node][i]] == 0) {
				qq.push(graph[node][i]);
				marked[graph[node][i]] = 1;
			}
		}
	}
}

void p_bfs()
{
	omp_set_num_threads(threads_num);

	omp_lock_t lck;
	omp_init_lock(&lck);

	root = 0;
	qq.push(root);
	while (!qq.empty()) {

#pragma omp parallel
		{
#pragma omp single
			{
				node = qq.front();
				qq.pop();

				marked[node] = 2;
			}

#pragma omp barrier

#pragma omp parallel for
			for (i = 0; i < graph[node].size(); ++i) {
				omp_set_lock(&lck);
				if (marked[graph[node][i]] == 0) {
					qq.push(graph[node][i]);
					marked[graph[node][i]] = 1;
				}
				omp_unset_lock(&lck);
			}
		}
	}

	omp_destroy_lock(&lck);
	free(marked);
}

int main(int argc, char const* argv[])
{
	threads_num = NUM_THREADS;

	s_init();
	//display();

	/* Perform Parallel Breadth First Search */
	auto start = chrono::steady_clock::now();
	p_bfs();
	auto end = chrono::steady_clock::now();
	auto dur = chrono::duration_cast<chrono::milliseconds>(end - start).count();
	cout << "\Parallel BFS: " << dur << "ms. with " << NUM_THREADS << " th; \n\n";
	graph.clear();

	return 0;
}